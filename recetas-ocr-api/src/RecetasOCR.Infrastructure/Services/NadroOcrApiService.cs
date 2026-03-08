using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Ocr;
using RecetasOCR.Domain.Common;
using RecetasOCR.Infrastructure.Persistence.Entities;

namespace RecetasOCR.Infrastructure.Services;

/// <summary>
/// Implementación de IOcrApiService que consume la API de Nadro OCR.
/// Endpoint: POST https://concordia.nadro.dev/api/batch_recetas_medicas
///
/// NOTA: El HttpClient "NadroOcrClient" debe registrarse en Infrastructure DI con:
///   - Polly WaitAndRetryAsync: 3 reintentos, backoff 2s / 4s / 8s
///   - Polly CircuitBreakerAsync: 5 fallos consecutivos → abre 30 s
///   - client.Timeout = 120 s
/// </summary>
public class NadroOcrApiService : IOcrApiService
{
    private const string EndpointUrl  = "https://concordia.nadro.dev/api/batch_recetas_medicas";
    private const string ModeloNadro  = "gemini-2.5-flash-preview-05-20";
    private const string ProveedorNadro = "gemini";

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly IRecetasOcrDbContext   _ctx;
    private readonly IBlobStorageService    _blob;
    private readonly IParametrosService     _parametros;
    private readonly IHttpClientFactory     _httpFactory;
    private readonly ILogger<NadroOcrApiService> _logger;

    public NadroOcrApiService(
        IRecetasOcrDbContext ctx,
        IBlobStorageService blob,
        IParametrosService parametros,
        IHttpClientFactory httpFactory,
        ILogger<NadroOcrApiService> logger)
    {
        _ctx        = ctx;
        _blob       = blob;
        _parametros = parametros;
        _httpFactory = httpFactory;
        _logger     = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    public async Task<OcrResultadoDto> ProcesarImagenAsync(
        string urlBlobRaw,
        Guid   idImagen,
        CancellationToken ct = default)
    {
        try
        {
            // ── Cargar configuración OCR principal ──────────────────────────────
            var config = await _ctx.Set<ConfiguracionesOcr>()
                .Where(c => c.EsPrincipal && c.Activo)
                .FirstOrDefaultAsync(ct);

            if (config is null)
            {
                _logger.LogError("No se encontró cfg.ConfiguracionesOCR activa con EsPrincipal=1.");
                return FailDto("Configuración OCR no encontrada.");
            }

            // ── Cargar entidades del dominio ────────────────────────────────────
            var imagen = await _ctx.Set<Imagene>()
                             .FirstOrDefaultAsync(i => i.Id == idImagen, ct)
                         ?? throw new InvalidOperationException($"Imagen {idImagen} no encontrada.");

            var grupo = await _ctx.Set<GruposRecetum>()
                            .FirstOrDefaultAsync(g => g.Id == imagen.IdGrupo, ct)
                        ?? throw new InvalidOperationException($"Grupo {imagen.IdGrupo} no encontrado.");

            // ── PASO 1 — Descargar blob y convertir a Base64 ────────────────────
            var nombreArchivo = Path.GetFileName(new Uri(urlBlobRaw).AbsolutePath);
            var ext      = Path.GetExtension(nombreArchivo).ToLowerInvariant();
            var mimeType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png"            => "image/png",
                ".pdf"            => "application/pdf",
                ".heic"           => "image/heic",
                _                 => "image/jpeg"
            };

            string base64;
            using (var blobStream = await _blob.DescargarAsync(urlBlobRaw, ct))
            using (var ms = new MemoryStream())
            {
                await blobStream.CopyToAsync(ms, ct);
                base64 = Convert.ToBase64String(ms.ToArray());
            }

            // ── PASO 2 — Construir request batch ────────────────────────────────
            var requestBody = new
            {
                files = new[]
                {
                    new { name = nombreArchivo, data = base64 }
                },
                model       = ModeloNadro,
                provider    = ProveedorNadro,
                concurrency = 1,
                type        = "receta"
            };

            // ── PASO 3 — POST con HttpClient configurado con Polly ──────────────
            // NOTA: ApiKeyEncriptada puede requerir descifrado si se almacena cifrada en BD.
            var http = _httpFactory.CreateClient("NadroOcrClient");
            http.DefaultRequestHeaders.Remove("X-API-Key");
            http.DefaultRequestHeaders.Add("X-API-Key", config.ApiKeyEncriptada ?? "");

            var fechaPeticion = DateTime.UtcNow;
            var httpResponse  = await http.PostAsJsonAsync(EndpointUrl, requestBody, ct);
            var fechaRespuesta = DateTime.UtcNow;
            var duracionMs    = (int)(fechaRespuesta - fechaPeticion).TotalMilliseconds;

            var responseJson = await httpResponse.Content.ReadAsStringAsync(ct);

            // ── PASO 4 — Parsear NadroBatchResponseDto ───────────────────────────
            NadroBatchResponseDto? batchResponse = null;
            try
            {
                batchResponse = JsonSerializer.Deserialize<NadroBatchResponseDto>(responseJson, JsonOpts);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializando respuesta Nadro OCR para imagen {IdImagen}", idImagen);
            }

            var fileResult  = batchResponse?.Results?.FirstOrDefault();
            bool ocrExitoso = httpResponse.IsSuccessStatusCode
                              && batchResponse?.Success == true
                              && fileResult?.Success == true;

            // ── PASO 5 — Determinar legibilidad ──────────────────────────────────
            var pagina   = fileResult?.Data?.Paginas?.FirstOrDefault();
            var metadata = fileResult?.Data?.Metadata;
            var metaLect = pagina?.MetadataLectura;

            decimal confianza = metaLect?.PorcentajeLectura ?? 0;
            decimal umbral    = await _parametros.ObtenerDecimalAsync(
                                    Constantes.Parametros.OCR_CONFIANZA_MINIMA, 70m, ct);

            bool esLegible       = !string.Equals(metaLect?.CalidadLectura, "ilegible",
                                        StringComparison.OrdinalIgnoreCase);
            bool esBajaConfianza = confianza < umbral;

            var camposIlegibles   = metaLect?.CamposIlegibles ?? new List<string>();
            var motivoBajaCalidad = string.Join(", ",
                camposIlegibles.Where(c => !string.IsNullOrWhiteSpace(c)));
            if (!string.IsNullOrWhiteSpace(metaLect?.Notas))
                motivoBajaCalidad = string.IsNullOrEmpty(motivoBajaCalidad)
                    ? metaLect.Notas
                    : $"{motivoBajaCalidad}. {metaLect.Notas}";

            // ── PASO 6 — INSERT ocr.ResultadosOCR ────────────────────────────────
            var tokensInput  = metadata?.TokensInput  ?? 0;
            var tokensOutput = metadata?.TokensOutput ?? 0;
            var costo = (tokensInput * 0.000001m) + (tokensOutput * 0.000003m);

            var resultadoOcr = new ResultadosOcr
            {
                IdImagen           = idImagen,
                IdConfiguracionOcr = config.Id,
                ProveedorOcr       = metadata?.Proveedor ?? config.Proveedor,
                ModeloUsado        = metadata?.Modelo    ?? config.Modelo,
                UrlEndpointLlamado = EndpointUrl,
                FechaPeticion      = fechaPeticion,
                FechaRespuesta     = fechaRespuesta,
                DuracionMs         = duracionMs,
                TextoCompleto      = pagina is not null ? JsonSerializer.Serialize(pagina) : null,
                ConfianzaPromedio  = ocrExitoso ? confianza : null,
                IdiomaDetectado    = "spa",
                PaginasProcesadas  = 1,
                ResponseJsonCompleto = responseJson,
                CostoEstimadoUsd   = costo,
                FechaProceso       = fechaRespuesta,
                Exitoso            = ocrExitoso,
                CodigoErrorHttp    = ocrExitoso ? null : (int?)httpResponse.StatusCode,
                MensajeError       = ocrExitoso ? null
                                     : (fileResult?.Error ?? $"HTTP {(int)httpResponse.StatusCode}"),
                FechaModificacion  = fechaRespuesta
            };
            _ctx.Set<ResultadosOcr>().Add(resultadoOcr);

            if (!ocrExitoso)
            {
                imagen.EsLegible          = false;
                imagen.ScoreLegibilidad   = 0;
                imagen.FechaActualizacion = DateTime.UtcNow;
                imagen.FechaModificacion  = DateTime.UtcNow;
                await _ctx.SaveChangesAsync(ct);

                return FailDto(fileResult?.Error ?? $"HTTP {(int)httpResponse.StatusCode}");
            }

            // ── PASO 7 — INSERT ocr.ResultadosExtraccion ─────────────────────────
            // IdResultadoOcrNavigation permite que EF resuelva el FK tras SaveChanges.
            var resultadoExtraccion = new ResultadosExtraccion
            {
                IdImagen                = idImagen,
                IdResultadoOcrNavigation = resultadoOcr,
                IdConfiguracionOcr      = config.Id,
                Motor                   = "API_EXTERNA_OCR",
                Jsonestructurado        = pagina is not null ? JsonSerializer.Serialize(pagina) : null,
                ConfianzaExtraccion     = confianza,
                CamposFaltantes         = string.Join(", ", camposIlegibles),
                AseguradoraDetectada    = pagina?.AseguradoraDetectada,
                FormatoDetectado        = pagina?.FormatoReceta,
                TokensEntrada           = tokensInput,
                TokensSalida            = tokensOutput,
                CostoEstimadoUsd        = costo,
                FechaProceso            = fechaRespuesta,
                Exitoso                 = true,
                FechaModificacion       = fechaRespuesta
            };
            _ctx.Set<ResultadosExtraccion>().Add(resultadoExtraccion);

            // ── PASO 8 — UPDATE rec.GruposReceta (solo campos NOT NULL) ──────────
            var paciente = pagina?.Paciente;
            var medico   = pagina?.Medico;
            var consulta = pagina?.Consulta;

            if (paciente?.NombreCompleto     is not null) grupo.NombrePaciente       = paciente.NombreCompleto;
            if (paciente?.ApellidoPaterno    is not null) grupo.ApellidoPaterno      = paciente.ApellidoPaterno;
            if (paciente?.ApellidoMaterno    is not null) grupo.ApellidoMaterno      = paciente.ApellidoMaterno;
            if (paciente?.Nombres            is not null) grupo.NombrePac            = paciente.Nombres;
            if (paciente?.Nomina             is not null) grupo.NominaPaciente       = paciente.Nomina;
            if (paciente?.Credencial         is not null) grupo.Credencial           = paciente.Credencial;
            if (paciente?.Nur                is not null) grupo.Nur                  = paciente.Nur;
            if (paciente?.NumeroAutorizacion is not null) grupo.NumeroAutorizacion   = paciente.NumeroAutorizacion;
            if (paciente?.Elegibilidad       is not null) grupo.Elegibilidad         = paciente.Elegibilidad;
            if (paciente?.ClaveDh            is not null) grupo.ClaveDh             = paciente.ClaveDh;
            if (paciente?.ClaveBeneficiario  is not null) grupo.ClaveBeneficiario   = paciente.ClaveBeneficiario;

            if (medico?.NombreCompleto    is not null) grupo.NombreMedico          = medico.NombreCompleto;
            if (medico?.Nombres           is not null) grupo.NombreMedicoNombre    = medico.Nombres;
            if (medico?.ApellidoPaterno   is not null) grupo.ApellidoPaternoMedico = medico.ApellidoPaterno;
            if (medico?.ApellidoMaterno   is not null) grupo.ApellidoMaternoMedico = medico.ApellidoMaterno;
            if (medico?.CedulaProfesional is not null) grupo.CedulaMedico          = medico.CedulaProfesional;
            if (medico?.ClaveMedico       is not null) grupo.ClaveMedico           = medico.ClaveMedico;
            if (medico?.Especialidad      is not null) grupo.EspecialidadTexto     = medico.Especialidad;
            if (medico?.Institucion       is not null) grupo.InstitucionMedico     = medico.Institucion;
            if (medico?.Direccion         is not null) grupo.DireccionMedico       = medico.Direccion;
            if (medico?.Telefono          is not null) grupo.TelefonoMedico        = medico.Telefono;

            if (consulta?.DiagnosticoTexto is not null) grupo.DescripcionDiagnostico = consulta.DiagnosticoTexto;
            if (consulta?.CodigoCie10      is not null) grupo.CodigoCie10            = consulta.CodigoCie10;

            if (consulta?.Fecha is not null && DateOnly.TryParse(consulta.Fecha, out var fechaConsulta))
                grupo.FechaConsulta = fechaConsulta;
            if (consulta?.Hora is not null && TimeOnly.TryParse(consulta.Hora, out var horaConsulta))
                grupo.HoraConsulta = horaConsulta;

            grupo.FechaActualizacion = DateTime.UtcNow;
            grupo.FechaModificacion  = DateTime.UtcNow;

            // ── PASO 9 — INSERT med.MedicamentosReceta ───────────────────────────
            var medicamentosNadro = pagina?.Medicamentos ?? new List<NadroMedicamentoDto>();

            // Pre-cargar vías de administración para evitar N+1 queries
            var viasClaves = medicamentosNadro
                .Where(m => !string.IsNullOrWhiteSpace(m.ViaAdministracion))
                .Select(m => m.ViaAdministracion!)
                .Distinct()
                .ToList();

            var viasDb = await _ctx.Set<ViasAdministracion>()
                .Where(v => viasClaves.Contains(v.Clave))   // CI_AI collation maneja el case
                .Select(v => new { v.Clave, v.Id })
                .ToListAsync(ct);

            var viaDict = viasDb.ToDictionary(
                v => v.Clave,
                v => v.Id,
                StringComparer.OrdinalIgnoreCase);

            foreach (var med in medicamentosNadro)
            {
                // Buscar en catálogo por nombre comercial (null si no existe — revisor lo completa)
                int? idCatalogo = null;
                if (!string.IsNullOrWhiteSpace(med.NombreComercial))
                {
                    var nombre = med.NombreComercial;
                    idCatalogo = await _ctx.Set<Medicamento>()
                        .Where(m => m.NombreComercial.Contains(nombre))
                        .Select(m => (int?)m.Id)
                        .FirstOrDefaultAsync(ct);
                }

                int? idVia = null;
                if (!string.IsNullOrWhiteSpace(med.ViaAdministracion) &&
                    viaDict.TryGetValue(med.ViaAdministracion, out var viaIdVal))
                    idVia = viaIdVal;

                _ctx.Set<MedicamentosRecetum>().Add(new MedicamentosRecetum
                {
                    IdImagen              = idImagen,
                    IdGrupo               = imagen.IdGrupo,
                    IdMedicamentoCatalogo = idCatalogo,
                    NumeroPrescripcion    = med.Numero,
                    NombreComercial       = med.NombreComercial,
                    SustanciaActiva       = med.SustanciaActiva,
                    Presentacion          = med.Presentacion,
                    Dosis                 = med.Dosis,
                    CodigoCie10           = med.CodigoCie10,
                    CodigoEan             = med.CodigoEan,
                    CantidadTexto         = med.CantidadTexto,
                    CantidadNumero        = med.CantidadNumero,
                    UnidadCantidad        = med.UnidadCantidad,
                    IdViaAdministracion   = idVia,
                    FrecuenciaTexto       = med.FrecuenciaTexto,
                    FrecuenciaExpandida   = med.FrecuenciaExpandida,
                    DuracionTexto         = med.DuracionTexto,
                    DuracionDias          = med.DuracionDias,
                    IndicacionesCompletas = med.Indicaciones,
                    NumeroAutorizacion    = med.NumeroAutorizacion,
                    Ivatasa               = 0.16m,
                    Iepstasa              = 0m,
                    FechaCreacion         = DateTime.UtcNow,
                    FechaActualizacion    = DateTime.UtcNow,
                    FechaModificacion     = DateTime.UtcNow
                });
            }

            // Actualizar imagen con resultado OCR
            imagen.EsLegible          = esLegible;
            imagen.ScoreLegibilidad   = confianza;
            imagen.MotivoBajaCalidad  = esBajaConfianza
                                         ? (motivoBajaCalidad.Length > 0 ? motivoBajaCalidad : null)
                                         : null;
            imagen.FechaActualizacion = DateTime.UtcNow;
            imagen.FechaModificacion  = DateTime.UtcNow;

            // ── PASO 10 — UPDATE grupo.TotalMedicamentos ─────────────────────────
            // CountAsync ejecuta SQL sobre lo ya persistido; sumamos los recién agregados al tracker.
            var medsExistentes = await _ctx.Set<MedicamentosRecetum>()
                .CountAsync(m => m.IdGrupo == imagen.IdGrupo, ct);
            grupo.TotalMedicamentos = medsExistentes + medicamentosNadro.Count;

            // ── PASO 11 — SaveChangesAsync (única escritura para toda la operación) ─
            await _ctx.SaveChangesAsync(ct);

            // ── PASO 12 — Retornar OcrResultadoDto completo ──────────────────────
            return new OcrResultadoDto(
                Exitoso:             true,
                EsLegible:           esLegible,
                EsConfianzaBaja:     esBajaConfianza,
                ConfianzaPromedio:   confianza,
                TextoCompleto:       resultadoOcr.TextoCompleto,
                MotivoBajaCalidad:   motivoBajaCalidad.Length > 0 ? motivoBajaCalidad : null,
                ResponseJsonCompleto: responseJson,
                CostoEstimadoUsd:    costo,
                MensajeError:        null);
        }
        catch (Exception ex)
        {
            // NO relanzar — el Worker decide si reintentar vía ocr.ColaProcesamiento.Intentos.
            _logger.LogError(ex,
                "NadroOcrApiService.ProcesarImagenAsync falló para imagen {IdImagen}: {Message}",
                idImagen, ex.Message);
            return FailDto(ex.Message);
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static OcrResultadoDto FailDto(string mensajeError) => new(
        Exitoso:             false,
        EsLegible:           false,
        EsConfianzaBaja:     true,
        ConfianzaPromedio:   0m,
        TextoCompleto:       null,
        MotivoBajaCalidad:   null,
        ResponseJsonCompleto: null,
        CostoEstimadoUsd:    0m,
        MensajeError:        mensajeError);
}
