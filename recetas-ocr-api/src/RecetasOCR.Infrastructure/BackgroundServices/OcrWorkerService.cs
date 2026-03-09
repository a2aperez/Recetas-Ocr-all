using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Infrastructure.Persistence;
using RecetasOCR.Infrastructure.Persistence.Entities;

namespace RecetasOCR.Infrastructure.BackgroundServices;

/// <summary>
/// Worker de REINTENTOS: hace polling a ocr.ColaProcesamiento cada N segundos (cfg: OCR_WORKER_POLLING_SEG, default 60).
/// Solo recupera imágenes atascadas: PENDIENTE + Bloqueado=false + FechaEncolado &gt; 5 min.
/// El flujo principal de OCR lo ejecuta SubirImagenCommandHandler de forma síncrona.
/// Este worker es el safety net para imágenes que fallaron en el flujo síncrono.
///
/// FLUJO POR IMAGEN:
///  1. Tomar item PENDIENTE con más de 5 minutos sin procesar (bloqueo optimista)
///  2. Llamar a IOcrApiService.ProcesarImagenAsync con UrlBlobRaw
///  3. Si legible   → subir a recetas-ocr,        estado = OCR_APROBADO | OCR_BAJA_CONFIANZA
///  4. Si ilegible  → subir a recetas-ilegibles,   estado = ILEGIBLE  (raw NO se elimina)
///  5. Insertar en aud.HistorialEstadosImagen y aud.LogProcesamiento (Mensaje="Reintento Worker")
///  6. Actualizar ocr.ColaProcesamiento (COMPLETADO | FALLIDO)
/// </summary>
public class OcrWorkerService : BackgroundService
{
    private readonly IServiceScopeFactory     _scopeFactory;
    private readonly ILogger<OcrWorkerService> _logger;

    // Nombre único de esta instancia (para bloqueo optimista)
    private readonly string _workerName =
        $"WORKER-{Environment.MachineName}-{Guid.NewGuid().ToString("N")[..8]}";

    public OcrWorkerService(IServiceScopeFactory scopeFactory, ILogger<OcrWorkerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OcrWorkerService iniciado. WorkerName: {Worker}", _workerName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarSiguienteAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado en OcrWorkerService");
            }

            // Intervalo leído de cfg.Parametros[OCR_WORKER_POLLING_SEG] en cada ciclo
            // → permite cambio en caliente desde la BD sin reiniciar el worker.
            int pollingSegs;
            try
            {
                using var pollingScope = _scopeFactory.CreateScope();
                var p = pollingScope.ServiceProvider.GetRequiredService<IParametrosService>();
                pollingSegs = await p.ObtenerIntAsync("OCR_WORKER_POLLING_SEG", 60, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo leer el parámetro OCR_WORKER_POLLING_SEG; usando valor por defecto de 60 s.");
                pollingSegs = 60;
            }

            await Task.Delay(TimeSpan.FromSeconds(pollingSegs), stoppingToken);
        }
    }

    // Exposed as internal to allow unit-testing via a testable subclass.
    internal async Task ProcesarSiguienteAsync(CancellationToken ct)
    {
        _logger.LogDebug("Worker reintento: buscando imágenes atascadas > 5 min");

        using var scope         = _scopeFactory.CreateScope();
        var db                  = scope.ServiceProvider.GetRequiredService<RecetasOcrDbContext>();
        var ocrApiService       = scope.ServiceProvider.GetRequiredService<IOcrApiService>();
        var blobStorage         = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
        var parametros          = scope.ServiceProvider.GetRequiredService<IParametrosService>();

        // ── 1. Tomar siguiente item PENDIENTE atascado (más de 5 min sin procesar) ────
        // Imágenes nuevas las procesa SubirImagenCommandHandler síncronamente.
        // Este worker solo recupera las que fallaron (FechaEncolado > 5 min atrás).
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        var item = await db.ColaProcesamientos
            .Where(c => c.EstadoCola == "PENDIENTE" && !c.Bloqueado && c.FechaEncolado < cutoff)
            .OrderBy(c => c.Prioridad)
            .ThenBy(c => c.FechaEncolado)
            .FirstOrDefaultAsync(ct);

        if (item is null) return; // cola vacía

        // ── 2. Bloqueo optimista ────────────────────────────────────────────────
        var rows = await TryAcquireLockAsync(db, item.Id, ct);

        if (rows == 0) return; // otro worker lo tomó

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // ── 3. Cargar imagen y grupo ─────────────────────────────────────────
            var imagen = await db.Imagenes.FindAsync([item.IdImagen], ct)
                ?? throw new InvalidOperationException(
                    $"Imagen {item.IdImagen} no encontrada para el item de cola {item.Id}.");

            var grupo = await db.GruposReceta.FindAsync([imagen.IdGrupo], ct)
                ?? throw new InvalidOperationException(
                    $"Grupo {imagen.IdGrupo} no encontrado.");

            var estadoAnteriorId = imagen.IdEstadoImagen;

            // ── 4. Descargar imagen del blob para reintento OCR ──────────────────
            byte[] imageBytes;
            string mimeType;
            {
                using var ms = new MemoryStream();
                await using var rawStream = await blobStorage.DescargarAsync(item.UrlBlobRaw, ct);
                await rawStream.CopyToAsync(ms, ct);
                imageBytes = ms.ToArray();
                var ext = Path.GetExtension(Path.GetFileName(
                    new Uri(item.UrlBlobRaw).AbsolutePath)).ToLowerInvariant();
                mimeType = ext switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png"            => "image/png",
                    ".pdf"            => "application/pdf",
                    ".heic"           => "image/heic",
                    _                 => "image/jpeg"
                };
            }

            // ── 5. Llamar al servicio OCR ─────────────────────────────────────────
            var resultado = await ocrApiService.ProcesarImagenAsync(
                item.UrlBlobRaw, item.IdImagen, imageBytes, mimeType, ct);

            // ── 6. Resolver estado y subir blob secundario ────────────────────────
            string estadoClave;

            if (resultado.Exitoso && resultado.EsLegible)
            {
                estadoClave = resultado.EsConfianzaBaja ? "OCR_BAJA_CONFIANZA" : "OCR_APROBADO";

                using var ocrStream = new MemoryStream(imageBytes);
                var urlOcr = await blobStorage.SubirOcrAsync(ocrStream, imagen.NombreArchivo, ct);

                imagen.UrlBlobOcr       = urlOcr;
                imagen.EsLegible        = true;
                imagen.ScoreLegibilidad = resultado.ConfianzaPromedio;
            }
            else if (resultado.Exitoso && !resultado.EsLegible)
            {
                estadoClave = "ILEGIBLE";

                using var ilegStream = new MemoryStream(imageBytes);
                var urlIlegible = await blobStorage.SubirIlegibleAsync(ilegStream, imagen.NombreArchivo, ct);

                imagen.UrlBlobIlegible   = urlIlegible;
                imagen.EsLegible         = false;
                imagen.MotivoBajaCalidad = resultado.Notas;

                // Grupo requiere captura manual
                var estadoGrupoId = await db.EstadosGrupos
                    .Where(e => e.Clave == "REQUIERE_CAPTURA_MANUAL")
                    .Select(e => e.Id)
                    .FirstOrDefaultAsync(ct);
                if (estadoGrupoId != 0)
                    grupo.IdEstadoGrupo = estadoGrupoId;
            }
            else
            {
                // ── OCR falló ────────────────────────────────────────────────────
                item.Intentos++;
                var maxIntentos = await parametros.ObtenerIntAsync("OCR_MAX_INTENTOS", 3, ct);
                item.ErrorMensaje = resultado.ErrorMensaje?[..Math.Min(500, resultado.ErrorMensaje?.Length ?? 0)];

                if (item.Intentos >= maxIntentos)
                {
                    item.EstadoCola = "FALLIDO";
                    item.Bloqueado  = false;
                    estadoClave     = "ILEGIBLE";
                    _logger.LogWarning(
                        "[Worker] {Worker} — Imagen {IdImagen} superó {Max} intentos. Marcada como FALLIDO.",
                        _workerName, item.IdImagen, maxIntentos);
                }
                else
                {
                    // Re-encolar para siguiente ciclo
                    item.EstadoCola        = "PENDIENTE";
                    item.Bloqueado         = false;
                    item.FechaModificacion = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                    _logger.LogWarning(
                        "[Worker] {Worker} — Imagen {IdImagen} OCR fallido. Intento {N}/{Max}. Re-encolando.",
                        _workerName, item.IdImagen, item.Intentos, maxIntentos);
                    return;
                }
            }

            // ── 6. Actualizar IdEstadoImagen ─────────────────────────────────────
            var nuevoEstadoId = await db.EstadosImagens
                .Where(e => e.Clave == estadoClave)
                .Select(e => e.Id)
                .FirstOrDefaultAsync(ct);

            if (nuevoEstadoId == 0)
                _logger.LogError("[Worker] Estado imagen '{Clave}' no encontrado en cat.EstadosImagen.", estadoClave);
            else
                imagen.IdEstadoImagen = nuevoEstadoId;

            imagen.ModificadoPor       = _workerName;
            imagen.FechaActualizacion  = DateTime.UtcNow;

            // Actualizar grupo con datos del OCR
            if (resultado.Exitoso)
            {
                grupo.NombrePaciente         ??= resultado.NombrePaciente;
                grupo.ApellidoPaterno        ??= resultado.ApellidoPaterno;
                grupo.ApellidoMaterno        ??= resultado.ApellidoMaterno;
                grupo.NombreMedico           ??= resultado.NombreMedico;
                grupo.CedulaMedico           ??= resultado.CedulaMedico;
                grupo.EspecialidadTexto      ??= resultado.EspecialidadMedico;
                grupo.DescripcionDiagnostico ??= resultado.DiagnosticoTexto;
                grupo.NominaPaciente         ??= resultado.Nomina;
                grupo.Credencial             ??= resultado.Credencial;
                grupo.Nur                    ??= resultado.NUR;
                grupo.NumeroAutorizacion     ??= resultado.NumeroAutorizacion;
                grupo.Elegibilidad           ??= resultado.Elegibilidad;

                // INSERT medicamentos extraídos
                foreach (var med in resultado.Medicamentos)
                {
                    db.MedicamentosReceta.Add(new MedicamentosRecetum
                    {
                        Id                    = Guid.NewGuid(),
                        IdImagen              = item.IdImagen,
                        IdGrupo               = imagen.IdGrupo,
                        NombreComercial       = med.NombreComercial,
                        SustanciaActiva       = med.SustanciaActiva,
                        Presentacion          = med.Presentacion,
                        Dosis                 = med.Dosis,
                        CantidadTexto         = med.CantidadTexto,
                        CantidadNumero        = med.CantidadNumero,
                        UnidadCantidad        = med.UnidadCantidad,
                        FrecuenciaTexto       = med.FrecuenciaTexto,
                        FrecuenciaExpandida   = med.FrecuenciaExpandida,
                        DuracionTexto         = med.DuracionTexto,
                        DuracionDias          = med.DuracionDias,
                        IndicacionesCompletas = med.Indicaciones,
                        CodigoCie10           = med.CodigoCIE10,
                        CodigoEan             = med.CodigoEAN,
                        FechaCreacion         = DateTime.UtcNow,
                        FechaActualizacion    = DateTime.UtcNow,
                        FechaModificacion     = DateTime.UtcNow
                    });
                }
            }

            grupo.FechaActualizacion   = DateTime.UtcNow;

            // ── 7. INSERT aud.HistorialEstadosImagen ─────────────────────────────
            if (nuevoEstadoId != 0)
            {
                db.HistorialEstadosImagens.Add(new HistorialEstadosImagen
                {
                    IdImagen      = item.IdImagen,
                    EstadoAnterior = estadoAnteriorId,
                    EstadoNuevo   = nuevoEstadoId,
                    Motivo        = $"OCR procesado por {_workerName}",
                    FechaCambio   = DateTime.UtcNow
                });
            }

            // ── 8. INSERT aud.LogProcesamiento Paso='OCR_FIN' ───────────────────
            sw.Stop();
            db.LogProcesamientos.Add(new LogProcesamiento
            {
                IdImagen   = item.IdImagen,
                IdGrupo    = imagen.IdGrupo,
                Paso       = "OCR_FIN",
                Nivel      = resultado.Exitoso ? "INFO" : "ERROR",
                Mensaje    = $"Reintento Worker | OCR {estadoClave} | Confianza: {resultado.ConfianzaPromedio:F2}%",
                Detalle    = resultado.ErrorMensaje,
                DuracionMs = (int)sw.ElapsedMilliseconds,
                Servidor   = _workerName,
                FechaEvento = DateTime.UtcNow
            });

            // ── 9. UPDATE cola → COMPLETADO / FALLIDO ───────────────────────────
            item.EstadoCola       = resultado.Exitoso ? "COMPLETADO" : "FALLIDO";
            item.Bloqueado        = false;
            item.FechaFinProceso  = DateTime.UtcNow;
            item.FechaModificacion = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[Worker] {Worker} — Imagen {IdImagen} → {Estado} ({Ms} ms)",
                _workerName, item.IdImagen, estadoClave, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Worker] {Worker} — Error procesando imagen {IdImagen} (cola {ColaId})",
                _workerName, item.IdImagen, item.Id);

            // Liberar bloqueo e incrementar intentos para que otro worker reintente
        // (separate try/catch so a failure here doesn't swallow the original exception)
            try
            {
                await db.Database.ExecuteSqlAsync($"""
                    UPDATE ocr.ColaProcesamiento
                    SET    Bloqueado          = 0,
                           EstadoCola         = 'PENDIENTE',
                           Intentos           = Intentos + 1,
                           ErrorMensaje       = {ex.Message[..Math.Min(500, ex.Message.Length)]},
                           WorkerProcesando   = NULL,
                           FechaModificacion  = GETUTCDATE()
                    WHERE  Id = {item.Id}
                    """, ct);
            }
            catch (Exception releaseEx)
            {
                _logger.LogError(releaseEx,
                    "[Worker] {Worker} — No se pudo liberar bloqueo del item {ColaId}",
                    _workerName, item.Id);
            }
        }
    }

    /// <summary>
    /// Atomically acquires the optimistic lock on a queue item.
    /// Virtual so that tests can override it (InMemory provider does not support raw SQL).
    /// Returns rows affected: 1 = lock acquired, 0 = taken by another worker.
    /// </summary>
    protected virtual Task<int> TryAcquireLockAsync(
        RecetasOcrDbContext db, long itemId, CancellationToken ct)
        => db.Database.ExecuteSqlAsync($"""
            UPDATE ocr.ColaProcesamiento
            SET    Bloqueado           = 1,
                   WorkerProcesando   = {_workerName},
                   FechaBloqueo       = GETUTCDATE(),
                   EstadoCola         = 'PROCESANDO',
                   FechaInicioProceso = GETUTCDATE(),
                   FechaModificacion  = GETUTCDATE()
            WHERE  Id       = {itemId}
              AND  Bloqueado = 0
            """, ct);
}
