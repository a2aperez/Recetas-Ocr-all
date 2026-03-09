using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Common;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Imagenes;

/// <summary>
/// Handler de SubirImagenCommand — 7 pasos en orden estricto.
///
/// PASO 1: Validar que rec.GruposReceta existe y no está en RECHAZADO ni FACTURADA.
/// PASO 2: IBlobStorageService.SubirRawAsync → UrlBlobRaw.
///         REGLA: toda imagen va SIEMPRE a recetas-raw (UrlBlobRaw NOT NULL en BD).
/// PASO 3: INSERT rec.Imagenes con IdEstadoImagen=RECIBIDA, NumeroHoja=COUNT+1.
/// PASO 4: INSERT ocr.ColaProcesamiento (EstadoCola=PENDIENTE, Prioridad=5).
///         NO se llama IOcrApiService — el OcrWorkerService procesa la cola.
/// PASO 5: UPDATE rec.GruposReceta SET TotalImagenes = TotalImagenes + 1.
/// PASO 6: INSERT aud.LogProcesamiento Paso='COLA'.
/// PASO 7: SaveChangesAsync → retornar Id de la imagen.
///
/// Los pasos 3-7 se ejecutan en una sola transacción. Si algo falla,
/// el blob ya subido queda huérfano pero eso es aceptable por diseño
/// (jamás se elimina un blob según la regla del sistema).
/// </summary>
public class SubirImagenCommandHandler(
    IRecetasOcrDbContext db,
    IBlobStorageService  blob,
    ICurrentUserService  currentUser,
    IParametrosService   parametros,
    ILogger<SubirImagenCommandHandler> logger)
    : IRequestHandler<SubirImagenCommand, Guid>
{
    private static readonly string[] _estadosFinalesGrupo = ["RECHAZADO", "FACTURADA"];

    public async Task<Guid> Handle(
        SubirImagenCommand command,
        CancellationToken  cancellationToken)
    {
        // ── Pre-validación: tamaño máximo desde cfg.Parametros ─────────
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
                entidad:          "GrupoReceta",
                estadoActual:     grupo.EstadoClave,
                estadosPermitidos: ["cualquier estado distinto a RECHAZADO y FACTURADA"]);

        // ── PASO 2: Subir SIEMPRE a recetas-raw ────────────────────────
        // UrlBlobRaw es NOT NULL en BD — esta subida es obligatoria.
        var nombreBlob = $"{command.IdGrupo}/{Guid.NewGuid()}_{command.NombreArchivo}";
        string urlBlobRaw;

        try
        {
            urlBlobRaw = await blob.SubirRawAsync(
                command.Archivo, nombreBlob, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new BlobStorageException(
                "SubirRawAsync", Constantes.BlobContainers.RAW, ex);
        }

        logger.LogInformation(
            "[Imagenes] Blob subido a raw | Grupo: {IdGrupo} | Blob: {Blob}",
            command.IdGrupo, urlBlobRaw);

        // ── Pasos 3-7 en transacción atómica con execution strategy ────────
        // El DbContext tiene SqlServerRetryingExecutionStrategy habilitado.
        // Para usar transacciones manuales, debemos usar CreateExecutionStrategy().
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // ── PASO 3a: IdEstadoImagen = cat.EstadosImagen WHERE Clave='RECIBIDA'
                var estadoRecibida = await db.Database
                    .SqlQuery<EstadoRow>($"""
                        SELECT Id, Clave FROM cat.EstadosImagen WHERE Clave = 'RECIBIDA'
                        """)
                    .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new EntidadNoEncontradaException("cat.EstadosImagen", "RECIBIDA");

                // ── PASO 3b: NumeroHoja = COUNT(imagenes del grupo) + 1
                var countRow = await db.Database
                    .SqlQuery<CountRow>($"""
                        SELECT COUNT(*) AS Valor
                        FROM   rec.Imagenes
                        WHERE  IdGrupo = {command.IdGrupo}
                        """)
                    .FirstAsync(cancellationToken);

                var ahora       = DateTime.UtcNow;
                var imagenId    = Guid.NewGuid();
                var numeroHoja  = countRow.Valor + 1;
                var extension   = Path.GetExtension(command.NombreArchivo)
                                      ?.TrimStart('.').ToUpperInvariant();
                var usuarioId   = currentUser.UserId!.Value;
                var username    = currentUser.Username;
                var origen      = command.OrigenImagen.ToUpperInvariant();

                // ── PASO 3c: INSERT rec.Imagenes ───────────────────────────
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

                // ── PASO 4: INSERT ocr.ColaProcesamiento ───────────────────
                // EstadoCola='PENDIENTE', Prioridad=5.
                // El OcrWorkerService tomará este registro — NO llamar IOcrApiService aquí.
                await db.Database.ExecuteSqlAsync($"""
                    INSERT INTO ocr.ColaProcesamiento
                        (IdImagen, UrlBlobRaw, Prioridad, Intentos, MaxIntentos,
                         FechaEncolado, Bloqueado, EstadoCola, ModificadoPor, FechaModificacion)
                    VALUES
                        ({imagenId}, {urlBlobRaw}, 5, 0, 3,
                         {ahora}, 0, 'PENDIENTE', {username}, {ahora})
                    """, cancellationToken);

                // ── PASO 5: grupo.TotalImagenes++ ─────────────────────────
                await db.Database.ExecuteSqlAsync($"""
                    UPDATE rec.GruposReceta
                    SET    TotalImagenes      = TotalImagenes + 1,
                           FechaActualizacion = {ahora},
                           ModificadoPor      = {username},
                           FechaModificacion  = {ahora}
                    WHERE  Id = {command.IdGrupo}
                    """, cancellationToken);

                // ── PASO 6: INSERT aud.LogProcesamiento Paso='COLA' ────────
                var mensajeLog = $"Imagen encolada para OCR. Hoja: {numeroHoja}. BlobRaw: {urlBlobRaw}";

                await db.Database.ExecuteSqlAsync($"""
                    INSERT INTO aud.LogProcesamiento
                        (IdImagen, IdGrupo, Paso, Nivel, Mensaje, Servidor, FechaEvento)
                    VALUES
                        ({imagenId}, {command.IdGrupo}, 'COLA', 'INFO',
                         {mensajeLog}, {Environment.MachineName}, {ahora})
                    """, cancellationToken);

                // ── PASO 7: SaveChangesAsync → commit → retornar Id ───────
                await db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                logger.LogInformation(
                    "[Imagenes] Imagen registrada | Id: {Id} | Grupo: {IdGrupo} | Hoja: {Hoja}",
                    imagenId, command.IdGrupo, numeroHoja);

                return imagenId;
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    // ── Tipos locales para resultados de SqlQuery ──────────────────────

    private sealed record GrupoRow(Guid Id, string EstadoClave, int TotalImagenes);

    private sealed record EstadoRow(int Id, string Clave);

    private sealed record CountRow(int Valor);
}
