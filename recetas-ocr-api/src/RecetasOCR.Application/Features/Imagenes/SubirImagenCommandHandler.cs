using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Imagenes;
using RecetasOCR.Domain.Common;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Imagenes;

/// <summary>
/// Handler de SubirImagenCommand — OCR síncrono inmediato al subir la imagen.
///
/// FLUJO (9 pasos):
/// PASO 1 — Validar grupo (no RECHAZADO ni FACTURADA).
/// PASO 2 — IBlobStorageService.SubirRawAsync → UrlBlobRaw (SIEMPRE, NOT NULL en BD).
/// PASO 3 — INSERT rec.Imagenes (estado=RECIBIDA) +
///           INSERT ocr.ColaProcesamiento (EstadoCola=PROCESANDO, Bloqueado=1).
///           SaveChangesAsync ANTES del OCR → imagen persiste aunque OCR falle.
/// PASO 4 — IOcrApiService.ProcesarImagenAsync (síncrono).
///           El servicio inserta ResultadosOCR/Extraccion, actualiza Grupo y Medicamentos.
///           Si lanza excepción → cola=PENDIENTE, retorna ImagenDto en estado RECIBIDA.
/// PASO 5 — Subir blob secundario (recetas-ocr o recetas-ilegibles) según legibilidad.
///           Si !Exitoso → cola=PENDIENTE, retorna ImagenDto en estado RECIBIDA.
/// PASO 6 — UPDATE rec.Imagenes (estado final, UrlBlobOCR/UrlBlobIlegible).
/// PASO 7 — UPDATE ocr.ColaProcesamiento (COMPLETADO, Bloqueado=0, FechaFinProceso).
///           UPDATE rec.GruposReceta (TotalImagenes, TotalMedicamentos).
/// PASO 8 — INSERT aud.LogProcesamiento (Paso=OCR_FIN).
/// PASO 9 — SaveChangesAsync → retornar ImagenDto con DatosOcr.
///
/// Si OCR falla en cualquier paso, la imagen queda en RECIBIDA con cola=PENDIENTE
/// y el OcrWorkerService la reintentará en su siguiente ciclo de polling.
/// </summary>
public class SubirImagenCommandHandler(
    IRecetasOcrDbContext db,
    IBlobStorageService  blob,
    IOcrApiService       ocrApiService,
    ICurrentUserService  currentUser,
    IParametrosService   parametros,
    ILogger<SubirImagenCommandHandler> logger)
    : IRequestHandler<SubirImagenCommand, ImagenDto>
{
    private static readonly string[] _estadosFinalesGrupo = ["RECHAZADO", "FACTURADA"];

    public async Task<ImagenDto> Handle(
        SubirImagenCommand command,
        CancellationToken  cancellationToken)
    {
        // ── Pre-validación: tamaño máximo ───────────────────────────────
        var maxMb = await parametros.ObtenerDecimalAsync(
            Constantes.Parametros.MAX_TAMANIO_IMAGEN_MB, 15m, cancellationToken);

        if (command.TamanioBytes > (long)(maxMb * 1024 * 1024))
            throw new InvalidOperationException(
                $"El archivo excede el tamaño máximo permitido de {maxMb} MB.");

        // ── PASO 1: Validar grupo existe y no está en estado final ──────
        var grupo = await db.Database
            .SqlQuery<GrupoRow>($"""
                SELECT g.Id, eg.Clave AS EstadoClave, g.TotalImagenes
                FROM   rec.GruposReceta  g
                INNER JOIN cat.EstadosGrupo eg ON eg.Id = g.IdEstadoGrupo
                WHERE  g.Id = {command.IdGrupo}
                """)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new EntidadNoEncontradaException("GrupoReceta", command.IdGrupo);

        if (_estadosFinalesGrupo.Contains(grupo.EstadoClave, StringComparer.OrdinalIgnoreCase))
            throw new EstadoInvalidoException(
                entidad:           "GrupoReceta",
                estadoActual:      grupo.EstadoClave,
                estadosPermitidos: ["cualquier estado distinto a RECHAZADO y FACTURADA"]);

        // ── PASO 2: Subir SIEMPRE a recetas-raw ────────────────────────
        // UrlBlobRaw es NOT NULL en BD — esta subida es obligatoria antes de todo lo demás.
        var nombreBlob = $"{command.IdGrupo}/{Guid.NewGuid()}_{command.NombreArchivo}";
        string urlBlobRaw;

        try
        {
            using var rawStream = new MemoryStream(command.ArchivoBytes);
            urlBlobRaw = await blob.SubirRawAsync(
                rawStream, nombreBlob, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new BlobStorageException("SubirRawAsync", Constantes.BlobContainers.RAW, ex);
        }

        logger.LogInformation(
            "[Imagenes] Blob raw subido | Grupo: {IdGrupo} | Blob: {Blob}",
            command.IdGrupo, urlBlobRaw);

        // ── PASO 3: INSERT rec.Imagenes (RECIBIDA) ──────────────────────
        var estadoRecibida = await db.Database
            .SqlQuery<EstadoRow>($"""
                SELECT Id, Clave FROM cat.EstadosImagen WHERE Clave = 'RECIBIDA'
                """)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new EntidadNoEncontradaException("cat.EstadosImagen", "RECIBIDA");

        var countRow = await db.Database
            .SqlQuery<CountRow>($"""
                SELECT COUNT(*) AS Valor FROM rec.Imagenes WHERE IdGrupo = {command.IdGrupo}
                """)
            .FirstAsync(cancellationToken);

        var ahora      = DateTime.UtcNow;
        var imagenId   = Guid.NewGuid();
        var numeroHoja = countRow.Valor + 1;
        var extension  = Path.GetExtension(command.NombreArchivo)?.TrimStart('.').ToUpperInvariant();
        var usuarioId  = currentUser.UserId!.Value;
        var username   = currentUser.Username;
        var origen     = command.OrigenImagen.ToUpperInvariant();

        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO rec.Imagenes
                (Id, IdGrupo, NumeroHoja, UrlBlobRaw, NombreArchivo,
                 TamanioBytes, FormatoImagen, OrigenImagen, IdUsuarioSubida,
                 FechaSubida, FechaActualizacion, IdEstadoImagen,
                 EsCapturaManual, IntentosProceso, ModificadoPor, FechaModificacion)
            VALUES
                ({imagenId}, {command.IdGrupo}, {numeroHoja}, {urlBlobRaw},
                 {command.NombreArchivo}, {command.TamanioBytes}, {extension},
                 {origen}, {usuarioId}, {ahora}, {ahora}, {estadoRecibida.Id},
                 0, 0, {username}, {ahora})
            """, cancellationToken);

        // Cola con PROCESANDO + Bloqueado=1 para que el Worker no tome este ítem
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO ocr.ColaProcesamiento
                (IdImagen, UrlBlobRaw, Prioridad, Intentos, MaxIntentos,
                 FechaEncolado, Bloqueado, EstadoCola, WorkerProcesando,
                 ModificadoPor, FechaModificacion)
            VALUES
                ({imagenId}, {urlBlobRaw}, 5, 0, 3,
                 {ahora}, 1, 'PROCESANDO', 'API_SYNC',
                 {username}, {ahora})
            """, cancellationToken);

        // Guardar ANTES del OCR → imagen persiste en RECIBIDA si OCR falla
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "[Imagenes] Imagen {Id} registrada (RECIBIDA). Iniciando OCR síncrono…",
            imagenId);

        // ── PASO 4: OCR síncrono ────────────────────────────────────────
        // NadroOcrApiService usa los bytes en memoria — sin re-descarga del blob.
        OcrResultadoDto resultado;
        try
        {
            resultado = await ocrApiService.ProcesarImagenAsync(
                urlBlobRaw, imagenId, command.ArchivoBytes, command.MimeType, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[Imagenes] OCR síncrono lanzó excepción para imagen {Id}. Worker reintentará.",
                imagenId);
            await LiberarColaAsync(imagenId, ex.Message, cancellationToken);
            return BuildImagenDtoRecibida(imagenId, command, urlBlobRaw, numeroHoja, ahora, origen, username);
        }

        // ── PASO 5: Subir blob secundario + determinar estado final ────
        string  estadoClave;
        string? urlBlobOcr      = null;
        string? urlBlobIlegible = null;

        if (resultado.Exitoso && resultado.EsLegible)
        {
            estadoClave = resultado.EsConfianzaBaja ? "OCR_BAJA_CONFIANZA" : "OCR_APROBADO";
            using var ocrStream = new MemoryStream(command.ArchivoBytes);
            urlBlobOcr = await blob.SubirOcrAsync(ocrStream, command.NombreArchivo, cancellationToken);
        }
        else if (resultado.Exitoso && !resultado.EsLegible)
        {
            estadoClave = "ILEGIBLE";
            using var ilegStream = new MemoryStream(command.ArchivoBytes);
            urlBlobIlegible = await blob.SubirIlegibleAsync(
                ilegStream, command.NombreArchivo, cancellationToken);

            var ahoraIlegible = DateTime.UtcNow;
            await db.Database.ExecuteSqlAsync($"""
                UPDATE rec.GruposReceta
                SET    IdEstadoGrupo = (
                           SELECT Id FROM cat.EstadosGrupo
                           WHERE  Clave = 'REQUIERE_CAPTURA_MANUAL'),
                       FechaActualizacion = {ahoraIlegible},
                       ModificadoPor      = {username},
                       FechaModificacion  = {ahoraIlegible}
                WHERE  Id = {command.IdGrupo}
                """, cancellationToken);
        }
        else
        {
            // !Exitoso → dejar en RECIBIDA, Worker reintentará
            logger.LogWarning(
                "[Imagenes] OCR síncrono no exitoso para imagen {Id}. Error: {Err}",
                imagenId, resultado.ErrorMensaje);
            await LiberarColaAsync(imagenId, resultado.ErrorMensaje, cancellationToken);
            return BuildImagenDtoRecibida(imagenId, command, urlBlobRaw, numeroHoja, ahora, origen, username);
        }

        var ahoraFin = DateTime.UtcNow;

        // ── Actualizar grupo con datos del OCR (antes de PASO 6) ────────────
        if (resultado.Exitoso)
        {
            await db.Database.ExecuteSqlAsync($"""
                UPDATE rec.GruposReceta
                SET    NombrePaciente         = COALESCE(NombrePaciente,        {resultado.NombrePaciente}),
                       ApellidoPaterno        = COALESCE(ApellidoPaterno,       {resultado.ApellidoPaterno}),
                       ApellidoMaterno        = COALESCE(ApellidoMaterno,       {resultado.ApellidoMaterno}),
                       NombreMedico           = COALESCE(NombreMedico,          {resultado.NombreMedico}),
                       CedulaMedico           = COALESCE(CedulaMedico,          {resultado.CedulaMedico}),
                       EspecialidadTexto      = COALESCE(EspecialidadTexto,     {resultado.EspecialidadMedico}),
                       DescripcionDiagnostico = COALESCE(DescripcionDiagnostico,{resultado.DiagnosticoTexto}),
                       NominaPaciente         = COALESCE(NominaPaciente,        {resultado.Nomina}),
                       Credencial             = COALESCE(Credencial,            {resultado.Credencial}),
                       NUR                    = COALESCE(NUR,                   {resultado.NUR}),
                       NumeroAutorizacion     = COALESCE(NumeroAutorizacion,    {resultado.NumeroAutorizacion}),
                       Elegibilidad           = COALESCE(Elegibilidad,          {resultado.Elegibilidad}),
                       FechaConsulta          = COALESCE(FechaConsulta, TRY_CAST({resultado.FechaConsulta} AS DATE)),
                       FechaModificacion      = {ahoraFin},
                       ModificadoPor          = {username}
                WHERE  Id = {command.IdGrupo}
                """, cancellationToken);

            // INSERT medicamentos extraídos por OCR
            foreach (var med in resultado.Medicamentos)
            {
                var medId = Guid.NewGuid();
                await db.Database.ExecuteSqlAsync($"""
                    INSERT INTO med.MedicamentosReceta
                        (Id, IdImagen, IdGrupo, NombreComercial, SustanciaActiva, Presentacion,
                         Dosis, CantidadTexto, CantidadNumero, UnidadCantidad,
                         FrecuenciaTexto, FrecuenciaExpandida, DuracionTexto, DuracionDias,
                         IndicacionesCompletas, CodigoCIE10, CodigoEAN, ModificadoPor, FechaModificacion)
                    VALUES
                        ({medId}, {imagenId}, {command.IdGrupo},
                         {med.NombreComercial}, {med.SustanciaActiva}, {med.Presentacion},
                         {med.Dosis}, {med.CantidadTexto}, {med.CantidadNumero}, {med.UnidadCantidad},
                         {med.FrecuenciaTexto}, {med.FrecuenciaExpandida}, {med.DuracionTexto}, {med.DuracionDias},
                         {med.Indicaciones}, {med.CodigoCIE10}, {med.CodigoEAN}, {username}, {ahoraFin})
                    """, cancellationToken);
            }
        }

        await db.Database.ExecuteSqlAsync($"""
            UPDATE rec.Imagenes
            SET    IdEstadoImagen     = (SELECT Id FROM cat.EstadosImagen WHERE Clave = {estadoClave}),
                   UrlBlobOCR         = {urlBlobOcr},
                   UrlBlobIlegible    = {urlBlobIlegible},
                   ModificadoPor      = {username},
                   FechaActualizacion = {ahoraFin},
                   FechaModificacion  = {ahoraFin}
            WHERE  Id = {imagenId}
            """, cancellationToken);

        // ── PASO 7: UPDATE cola COMPLETADO + UPDATE grupo totales ───────
        await db.Database.ExecuteSqlAsync($"""
            UPDATE ocr.ColaProcesamiento
            SET    EstadoCola        = 'COMPLETADO',
                   Bloqueado         = 0,
                   WorkerProcesando  = NULL,
                   FechaFinProceso   = {ahoraFin},
                   FechaModificacion = {ahoraFin}
            WHERE  IdImagen = {imagenId}
            """, cancellationToken);

        await db.Database.ExecuteSqlAsync($"""
            UPDATE rec.GruposReceta
            SET    TotalImagenes     = (SELECT COUNT(*)
                                        FROM   rec.Imagenes
                                        WHERE  IdGrupo = {command.IdGrupo}),
                   TotalMedicamentos = (SELECT COUNT(*)
                                        FROM   med.MedicamentosReceta
                                        WHERE  IdGrupo = {command.IdGrupo}),
                   FechaActualizacion = {ahoraFin},
                   ModificadoPor      = {username},
                   FechaModificacion  = {ahoraFin}
            WHERE  Id = {command.IdGrupo}
            """, cancellationToken);

        // ── PASO 8: INSERT aud.LogProcesamiento ─────────────────────────
        var mensajeLog =
            $"OCR síncrono: {estadoClave}. Confianza: {resultado.ConfianzaPromedio:F2}%";

        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO aud.LogProcesamiento
                (IdImagen, IdGrupo, Paso, Nivel, Mensaje, Servidor, FechaEvento)
            VALUES
                ({imagenId}, {command.IdGrupo},
                 'OCR_FIN', 'INFO',
                 {mensajeLog}, {Environment.MachineName}, {ahoraFin})
            """, cancellationToken);

        // ── PASO 9: SaveChangesAsync → retornar ImagenDto con DatosOcr ─
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "[Imagenes] OCR síncrono completado | Id: {Id} | Estado: {Estado} | Confianza: {Conf:F2}%",
            imagenId, estadoClave, resultado.ConfianzaPromedio);

        var datosOcr = BuildDatosOcr(resultado);

        return new ImagenDto(
            Id:               imagenId,
            IdGrupo:          command.IdGrupo,
            NumeroHoja:       numeroHoja,
            UrlBlobRaw:       urlBlobRaw,
            UrlBlobOcr:       urlBlobOcr,
            UrlBlobIlegible:  urlBlobIlegible,
            OrigenImagen:     origen,
            NombreArchivo:    command.NombreArchivo,
            TamanioBytes:     command.TamanioBytes,
            FechaSubida:      ahora,
            ScoreLegibilidad: resultado.ConfianzaPromedio > 0 ? resultado.ConfianzaPromedio : null,
            EsLegible:        resultado.EsLegible,
            MotivoBajaCalidad: resultado.Notas,
            EsCapturaManual:  false,
            EstadoImagen:     estadoClave,
            ModificadoPor:    username,
            FechaModificacion: ahoraFin,
            DatosOcr:         datosOcr
        );
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Libera el bloqueo de la cola para que el Worker pueda reintentar.
    /// </summary>
    private async Task LiberarColaAsync(
        Guid      imagenId,
        string?   mensajeError,
        CancellationToken ct)
    {
        var errTruncado = mensajeError is { } s
            ? s[..Math.Min(500, s.Length)]
            : null;
        var ahora = DateTime.UtcNow;
        try
        {
            await db.Database.ExecuteSqlAsync($"""
                UPDATE ocr.ColaProcesamiento
                SET    EstadoCola        = 'PENDIENTE',
                       Bloqueado         = 0,
                       WorkerProcesando  = NULL,
                       ErrorMensaje      = {errTruncado},
                       FechaModificacion = {ahora}
                WHERE  IdImagen = {imagenId}
                """, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[Imagenes] No se pudo liberar cola para imagen {Id}", imagenId);
        }
    }

    /// <summary>
    /// Construye un ImagenDto en estado RECIBIDA para retornar cuando el OCR falla.
    /// La imagen ya está persistida en BD; el Worker la reintentará.
    /// </summary>
    private static ImagenDto BuildImagenDtoRecibida(
        Guid               imagenId,
        SubirImagenCommand command,
        string             urlBlobRaw,
        int                numeroHoja,
        DateTime           ahora,
        string             origen,
        string?            username) => new(
        Id:               imagenId,
        IdGrupo:          command.IdGrupo,
        NumeroHoja:       numeroHoja,
        UrlBlobRaw:       urlBlobRaw,
        UrlBlobOcr:       null,
        UrlBlobIlegible:  null,
        OrigenImagen:     origen,
        NombreArchivo:    command.NombreArchivo,
        TamanioBytes:     command.TamanioBytes,
        FechaSubida:      ahora,
        ScoreLegibilidad: null,
        EsLegible:        null,
        MotivoBajaCalidad: null,
        EsCapturaManual:  false,
        EstadoImagen:     "RECIBIDA",
        ModificadoPor:    username,
        FechaModificacion: ahora,
        DatosOcr:         null
    );

    /// <summary>
    /// Construye DatosOcrExtraidosDto directamente del resultado OCR,
    /// sin consulta adicional a la BD.
    /// </summary>
    private static DatosOcrExtraidosDto? BuildDatosOcr(OcrResultadoDto resultado)
    {
        if (!resultado.Exitoso) return null;

        var calidad = resultado.CalidadLectura.Length > 0
            ? resultado.CalidadLectura.ToUpperInvariant()
            : resultado.ConfianzaPromedio >= 80m ? "ALTA"
              : resultado.ConfianzaPromedio >= 60m ? "MEDIA"
              : "BAJA";

        return new DatosOcrExtraidosDto
        {
            NombrePaciente     = resultado.NombrePaciente,
            ApellidoPaterno    = resultado.ApellidoPaterno,
            ApellidoMaterno    = resultado.ApellidoMaterno,
            Nomina             = resultado.Nomina,
            Credencial         = resultado.Credencial,
            NUR                = resultado.NUR,
            NumeroAutorizacion = resultado.NumeroAutorizacion,
            Elegibilidad       = resultado.Elegibilidad,
            NombreMedico       = resultado.NombreMedico,
            CedulaMedico       = resultado.CedulaMedico,
            Especialidad       = resultado.EspecialidadMedico,
            FechaConsulta      = resultado.FechaConsulta,
            DiagnosticoTexto   = resultado.DiagnosticoTexto,
            CodigoCIE10        = resultado.CodigoCIE10,
            Medicamentos       = resultado.Medicamentos,
            ConfianzaPromedio  = resultado.ConfianzaPromedio,
            CalidadLectura     = calidad,
            EsConfianzaBaja    = resultado.EsConfianzaBaja,
            CamposIlegibles    = resultado.CamposIlegibles,
            Notas              = resultado.Notas,
        };
    }

    // ── Tipos locales para SqlQuery projections ────────────────────────────

    private sealed record GrupoRow(Guid Id, string EstadoClave, int TotalImagenes);

    private sealed record EstadoRow(int Id, string Clave);

    private sealed record CountRow(int Valor);
}
