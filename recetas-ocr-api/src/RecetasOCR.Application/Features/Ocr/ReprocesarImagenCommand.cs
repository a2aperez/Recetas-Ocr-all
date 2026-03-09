using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Ocr;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Ocr;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Reprocesa una imagen de forma síncrona e inmediata.
/// Solo aplica si la imagen está en ILEGIBLE o OCR_BAJA_CONFIANZA.
/// </summary>
public record ReprocesarImagenCommand(
    Guid   IdImagen,
    string Motivo
) : IRequest<EstadoOcrDto>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class ReprocesarImagenCommandValidator : AbstractValidator<ReprocesarImagenCommand>
{
    public ReprocesarImagenCommandValidator()
    {
        RuleFor(x => x.IdImagen).NotEmpty();
        RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

/// <summary>
/// Handler de ReprocesarImagenCommand — OCR síncrono inmediato.
///
/// FLUJO:
/// PASO 1 — Validar imagen existe y estado es ILEGIBLE o OCR_BAJA_CONFIANZA.
/// PASO 2 — Protección anti-concurrencia: rechazar si ya hay un PROCESANDO activo.
/// PASO 3 — INSERT ocr.ColaProcesamiento (PROCESANDO, Bloqueado=1) + SaveChanges.
/// PASO 4 — IOcrApiService.ProcesarImagenAsync (síncrono).
///           Si lanza excepción → cola=PENDIENTE para que Worker reintente.
/// PASO 5 — Subir blob secundario (recetas-ocr o recetas-ilegibles) según legibilidad.
///           Si !Exitoso → cola=PENDIENTE para que Worker reintente.
/// PASO 6 — UPDATE rec.Imagenes (estado final, blobs, EsLegible, IntentosProceso++).
/// PASO 7 — UPDATE ocr.ColaProcesamiento (COMPLETADO, Bloqueado=0, FechaFinProceso).
///           UPDATE rec.GruposReceta (TotalMedicamentos).
/// PASO 8 — INSERT aud.LogProcesamiento Paso='OCR_FIN', Mensaje="Reproceso manual: {motivo}".
/// PASO 9 — SaveChangesAsync → retornar EstadoOcrDto actualizado.
/// </summary>
public class ReprocesarImagenCommandHandler(
    IRecetasOcrDbContext db,
    IBlobStorageService  blob,
    IOcrApiService       ocrApiService,
    ICurrentUserService  currentUser,
    IMediator            mediator,
    ILogger<ReprocesarImagenCommandHandler> logger)
    : IRequestHandler<ReprocesarImagenCommand, EstadoOcrDto>
{
    private static readonly string[] _estadosPermitidos = ["ILEGIBLE", "OCR_BAJA_CONFIANZA"];

    public async Task<EstadoOcrDto> Handle(
        ReprocesarImagenCommand command,
        CancellationToken       ct)
    {
        // ── PASO 1: Validar imagen y estado ───────────────────────────────
        var imagen = await db.Database
            .SqlQuery<ImagenRow>($"""
                SELECT i.Id, i.UrlBlobRaw, i.NombreArchivo, i.IdGrupo,
                       e.Clave AS EstadoImagen
                FROM   rec.Imagenes        i
                INNER  JOIN cat.EstadosImagen e ON e.Id = i.IdEstadoImagen
                WHERE  i.Id = {command.IdImagen}
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntidadNoEncontradaException("Imagen", command.IdImagen);

        if (!_estadosPermitidos.Contains(imagen.EstadoImagen, StringComparer.OrdinalIgnoreCase))
            throw new EstadoInvalidoException("Imagen", imagen.EstadoImagen, _estadosPermitidos);

        // ── PASO 2: Protección anti-concurrencia ──────────────────────────
        var esProcesando = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value
                FROM   ocr.ColaProcesamiento
                WHERE  IdImagen   = {command.IdImagen}
                  AND  EstadoCola = 'PROCESANDO'
                """)
            .FirstAsync(ct);

        if (esProcesando > 0)
            throw new InvalidOperationException(
                "La imagen ya está siendo procesada. Aguarde unos segundos e intente de nuevo.");

        var modificadoPor = currentUser.Username ?? "sistema";
        var ahora         = DateTime.UtcNow;

        // ── PASO 3: INSERT cola con PROCESANDO + Bloqueado=1 ─────────────
        // Siempre se inserta un registro nuevo (historial completo de reintentos).
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO ocr.ColaProcesamiento
                (IdImagen, UrlBlobRaw, Prioridad, Intentos, MaxIntentos,
                 FechaEncolado, Bloqueado, EstadoCola, WorkerProcesando,
                 ModificadoPor, FechaModificacion)
            VALUES
                ({command.IdImagen}, {imagen.UrlBlobRaw}, 1, 0, 3,
                 {ahora}, 1, 'PROCESANDO', 'API_REPROCESO',
                 {modificadoPor}, {ahora})
            """, ct);

        // Guardar ANTES del OCR → la entrada de cola queda registrada si OCR falla
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "[Ocr] Reproceso manual iniciado | Imagen: {Id} | Motivo: {Motivo}",
            command.IdImagen, command.Motivo);

        // ── PASO 4: OCR síncrono ──────────────────────────────────────────
        // Descargar bytes del blob para el reproceso
        byte[] imageBytes;
        {
            using var ms = new MemoryStream();
            await using var rawStream = await blob.DescargarAsync(imagen.UrlBlobRaw, ct);
            await rawStream.CopyToAsync(ms, ct);
            imageBytes = ms.ToArray();
        }
        var ext = Path.GetExtension(Path.GetFileName(
            new Uri(imagen.UrlBlobRaw).AbsolutePath)).ToLowerInvariant();
        var mimeType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"           => "image/png",
            ".pdf"           => "application/pdf",
            ".heic"          => "image/heic",
            _                => "image/jpeg"
        };

        OcrResultadoDto resultado;
        try
        {
            resultado = await ocrApiService.ProcesarImagenAsync(
                imagen.UrlBlobRaw, command.IdImagen, imageBytes, mimeType, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[Ocr] Reproceso manual lanzó excepción para imagen {Id}. Worker reintentará.",
                command.IdImagen);
            await LiberarColaAsync(command.IdImagen, ex.Message, ct);
            return await mediator.Send(new GetEstadoOcrQuery(command.IdImagen), ct);
        }

        // ── PASO 5: Subir blob secundario + determinar estado final ───────
        string  estadoClave;
        string? urlBlobOcr      = null;
        string? urlBlobIlegible = null;

        if (resultado.Exitoso && resultado.EsLegible)
        {
            estadoClave = resultado.EsConfianzaBaja ? "OCR_BAJA_CONFIANZA" : "OCR_APROBADO";
            using var ocrStream = new MemoryStream(imageBytes);
            urlBlobOcr = await blob.SubirOcrAsync(ocrStream, imagen.NombreArchivo, ct);
        }
        else if (resultado.Exitoso && !resultado.EsLegible)
        {
            estadoClave = "ILEGIBLE";
            using var ilegStream = new MemoryStream(imageBytes);
            urlBlobIlegible = await blob.SubirIlegibleAsync(ilegStream, imagen.NombreArchivo, ct);

            var ahoraIlegible = DateTime.UtcNow;
            await db.Database.ExecuteSqlAsync($"""
                UPDATE rec.GruposReceta
                SET    IdEstadoGrupo      = (
                           SELECT Id FROM cat.EstadosGrupo
                           WHERE  Clave = 'REQUIERE_CAPTURA_MANUAL'),
                       FechaActualizacion = {ahoraIlegible},
                       ModificadoPor      = {modificadoPor},
                       FechaModificacion  = {ahoraIlegible}
                WHERE  Id = {imagen.IdGrupo}
                """, ct);
        }
        else
        {
            logger.LogWarning(
                "[Ocr] Reproceso manual no exitoso para imagen {Id}. Error: {Err}",
                command.IdImagen, resultado.ErrorMensaje);
            await LiberarColaAsync(command.IdImagen, resultado.ErrorMensaje, ct);
            return await mediator.Send(new GetEstadoOcrQuery(command.IdImagen), ct);
        }

        var ahoraFin = DateTime.UtcNow;

        // ── PASO 6: UPDATE rec.Imagenes ───────────────────────────────────
        await db.Database.ExecuteSqlAsync($"""
            UPDATE rec.Imagenes
            SET    IdEstadoImagen     = (SELECT Id FROM cat.EstadosImagen WHERE Clave = {estadoClave}),
                   UrlBlobOCR         = {urlBlobOcr},
                   UrlBlobIlegible    = {urlBlobIlegible},
                   EsLegible          = {resultado.EsLegible},
                   ScoreLegibilidad   = {resultado.ConfianzaPromedio},
                   MotivoBajaCalidad  = {resultado.Notas},
                   IntentosProceso    = IntentosProceso + 1,
                   ModificadoPor      = {modificadoPor},
                   FechaActualizacion = {ahoraFin},
                   FechaModificacion  = {ahoraFin}
            WHERE  Id = {command.IdImagen}
            """, ct);

        // ── PASO 7: UPDATE cola COMPLETADO + UPDATE grupo medicamentos ────
        await db.Database.ExecuteSqlAsync($"""
            UPDATE ocr.ColaProcesamiento
            SET    EstadoCola        = 'COMPLETADO',
                   Bloqueado         = 0,
                   WorkerProcesando  = NULL,
                   FechaFinProceso   = {ahoraFin},
                   FechaModificacion = {ahoraFin}
            WHERE  IdImagen   = {command.IdImagen}
              AND  EstadoCola = 'PROCESANDO'
              AND  Bloqueado  = 1
            """, ct);

        await db.Database.ExecuteSqlAsync($"""
            UPDATE rec.GruposReceta
            SET    TotalMedicamentos  = (SELECT COUNT(*)
                                         FROM   med.MedicamentosReceta
                                         WHERE  IdGrupo = {imagen.IdGrupo}),
                   FechaActualizacion = {ahoraFin},
                   ModificadoPor      = {modificadoPor},
                   FechaModificacion  = {ahoraFin}
            WHERE  Id = {imagen.IdGrupo}
            """, ct);

        // ── PASO 8: INSERT aud.LogProcesamiento ───────────────────────────
        var mensajeLog =
            $"Reproceso manual: {command.Motivo} | {estadoClave} | Confianza: {resultado.ConfianzaPromedio:F2}%";

        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO aud.LogProcesamiento
                (IdImagen, IdGrupo, Paso, Nivel, Mensaje, Servidor, FechaEvento)
            VALUES
                ({command.IdImagen}, {imagen.IdGrupo},
                 'OCR_FIN', 'INFO',
                 {mensajeLog}, {Environment.MachineName}, {ahoraFin})
            """, ct);

        // ── PASO 9: SaveChangesAsync → retornar EstadoOcrDto actualizado ─
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "[Ocr] Reproceso manual completado | Id: {Id} | Estado: {Estado} | Confianza: {Conf:F2}%",
            command.IdImagen, estadoClave, resultado.ConfianzaPromedio);

        return await mediator.Send(new GetEstadoOcrQuery(command.IdImagen), ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task LiberarColaAsync(Guid imagenId, string? mensajeError, CancellationToken ct)
    {
        var errTruncado = mensajeError is { } s ? s[..Math.Min(500, s.Length)] : null;
        var ahora       = DateTime.UtcNow;
        try
        {
            await db.Database.ExecuteSqlAsync($"""
                UPDATE ocr.ColaProcesamiento
                SET    EstadoCola        = 'PENDIENTE',
                       Bloqueado         = 0,
                       WorkerProcesando  = NULL,
                       ErrorMensaje      = {errTruncado},
                       FechaModificacion = {ahora}
                WHERE  IdImagen   = {imagenId}
                  AND  EstadoCola = 'PROCESANDO'
                """, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[Ocr] No se pudo liberar cola para imagen {Id}", imagenId);
        }
    }

    private sealed record ImagenRow(
        Guid   Id,
        string UrlBlobRaw,
        string NombreArchivo,
        Guid   IdGrupo,
        string EstadoImagen);
}
