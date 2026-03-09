using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Imagenes;

namespace RecetasOCR.Infrastructure.Services;

/// <summary>
/// Implementación de IOcrApiService que llama directamente a la API de Nadro OCR
/// usando los bytes de la imagen en memoria (sin re-descarga del blob).
/// No accede a la base de datos — solo HTTP + parse JSON.
/// Registrada vía AddHttpClient&lt;IOcrApiService, NadroOcrApiService&gt;().
/// </summary>
public class NadroOcrApiService : IOcrApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NadroOcrApiService> _logger;
    private readonly IParametrosService _parametros;

    private const string ApiUrl = "https://concordia.nadro.dev/api/extract_receta_medica";
    private const string ApiKey = "ocr_71fc83371dd8c0931db6e20f1d05cf1d";

    private const string DefaultPrompt = @"Eres un sistema de OCR especializado en recetas médicas mexicanas del sector asegurador institucional.

Extrae TODOS los datos visibles de esta receta médica y devuelve ÚNICAMENTE un objeto JSON válido, sin texto adicional, sin explicaciones, sin markdown.

Estructura JSON requerida:
{
  ""aseguradora_detectada"": ""nombre o siglas de la aseguradora"",
  ""formato_receta"": ""tipo de formato detectado"",
  ""paciente"": {
    ""nombre_completo"": ""nombre tal como aparece"",
    ""apellido_paterno"": ""si es distinguible"",
    ""apellido_materno"": ""si es distinguible"",
    ""nombres"": ""nombre(s) de pila"",
    ""fecha_nacimiento"": ""YYYY-MM-DD o null"",
    ""nomina"": ""número de nómina si aparece"",
    ""credencial"": ""número de credencial si aparece"",
    ""nur"": ""Número Único de Receta si aparece"",
    ""numero_autorizacion"": ""si aparece"",
    ""elegibilidad"": ""tipo de elegibilidad si aparece"",
    ""clave_dh"": ""clave derechohabiente si aparece"",
    ""clave_beneficiario"": ""si aparece""
  },
  ""medico"": {
    ""nombre_completo"": ""nombre tal como aparece"",
    ""apellido_paterno"": ""si es distinguible"",
    ""apellido_materno"": ""si es distinguible"",
    ""nombres"": ""nombre(s) de pila"",
    ""cedula_profesional"": ""número de cédula"",
    ""clave_medico"": ""clave interna si aparece"",
    ""especialidad"": ""especialidad médica si aparece"",
    ""institucion"": ""nombre de la institución médica"",
    ""direccion"": ""dirección del consultorio si aparece"",
    ""telefono"": ""teléfono si aparece""
  },
  ""consulta"": {
    ""fecha"": ""YYYY-MM-DD"",
    ""hora"": ""HH:mm o null"",
    ""diagnostico_texto"": ""diagnóstico escrito si aparece"",
    ""codigo_cie10"": ""código CIE-10 si aparece""
  },
  ""medicamentos"": [
    {
      ""numero"": 1,
      ""nombre_comercial"": ""nombre del medicamento tal como aparece"",
      ""sustancia_activa"": ""principio activo si se menciona"",
      ""presentacion"": ""tabletas, cápsulas, jarabe, etc."",
      ""dosis"": ""100mg, 500mg, 5ml, etc."",
      ""cantidad_texto"": ""cantidad en letras si aparece"",
      ""cantidad_numero"": 30,
      ""unidad_cantidad"": ""tabletas, cápsulas, frascos, etc."",
      ""via_administracion"": ""oral, intramuscular, tópica, etc."",
      ""frecuencia_texto"": ""cada 8 horas, 3 veces al día, etc."",
      ""frecuencia_expandida"": ""Tomar 1 tableta cada 8 horas"",
      ""duracion_texto"": ""30 días, 2 semanas, etc."",
      ""duracion_dias"": 30,
      ""indicaciones"": ""indicaciones completas de uso"",
      ""codigo_cie10"": ""si aparece asociado al medicamento"",
      ""numero_autorizacion"": ""si aparece por medicamento"",
      ""codigo_ean"": ""código de barras si aparece""
    }
  ],
  ""metadata_lectura"": {
    ""calidad_lectura"": ""alta|media|baja"",
    ""porcentaje_lectura"": 85,
    ""campos_ilegibles"": [""lista de campos que no se pudieron leer""],
    ""notas"": ""observaciones sobre la calidad""
  }
}
REGLAS:
1. Devuelve SOLO el JSON, sin bloques de código, sin texto antes ni después.
2. Si un campo no es visible o legible, usa null.
3. Las fechas siempre en formato YYYY-MM-DD.
4. cantidad_numero y duracion_dias deben ser enteros.
5. porcentaje_lectura es tu estimación de 0 a 100.";

    public NadroOcrApiService(
        HttpClient httpClient,
        ILogger<NadroOcrApiService> logger,
        IParametrosService parametros)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMinutes(3);
        _logger     = logger;
        _parametros = parametros;
    }

    public async Task<OcrResultadoDto> ProcesarImagenAsync(
        string urlBlobRaw,
        Guid   idImagen,
        byte[] archivoBytes,
        string mimeType,
        CancellationToken ct = default)
    {
        // Cargar umbral de confianza desde parámetros (async, antes del bloque síncrono de parseo)
        var umbral = await _parametros.ObtenerDecimalAsync("OCR_UMBRAL_CONFIANZA", 70m, ct);

        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Iniciando OCR síncrono para imagen {Id} ({MimeType})",
                idImagen, mimeType);

            var base64  = Convert.ToBase64String(archivoBytes);
            var payload = new { file = base64, mime_type = mimeType, prompt = DefaultPrompt };

            var json    = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            request.Headers.TryAddWithoutValidation("X-API-Key", ApiKey);

            var response = await _httpClient.SendAsync(request, ct);
            var body     = await response.Content.ReadAsStringAsync(ct);

            sw.Stop();
            _logger.LogInformation(
                "OCR completado para {Id}: HTTP {Status} en {Ms}ms | Tamaño respuesta: {Size} bytes",
                idImagen, (int)response.StatusCode, sw.ElapsedMilliseconds, body.Length);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OCR falló HTTP {Status}: {Body}", (int)response.StatusCode, body);
                return OcrResultadoDto.Fallido($"Error HTTP {(int)response.StatusCode}");
            }

            // Logging de la respuesta completa en modo Debug para diagnóstico
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var bodyPreview = body.Length > 500 ? body[..500] + "..." : body;
                _logger.LogDebug(
                    "[OCR] Respuesta API para imagen {Id}: {Body}",
                    idImagen, bodyPreview);
            }

            return ParsearRespuestaOcr(body, idImagen, sw.ElapsedMilliseconds, umbral);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("OCR timeout para imagen {Id}", idImagen);
            return OcrResultadoDto.Fallido("Timeout — la solicitud excedió 3 minutos");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR excepción para imagen {Id}", idImagen);
            return OcrResultadoDto.Fallido(ex.Message);
        }
    }

    // ─── Parseo de la respuesta ────────────────────────────────────────────────

    private OcrResultadoDto ParsearRespuestaOcr(
        string body, Guid idImagen, long durationMs, decimal umbral)
    {
        try
        {
            // Limpiar posibles bloques markdown que la API pueda incluir
            var jsonText = body.Trim();
            if (jsonText.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                jsonText = jsonText[7..];
            if (jsonText.StartsWith("```"))
                jsonText = jsonText[3..];
            if (jsonText.EndsWith("```"))
                jsonText = jsonText[..^3];
            jsonText = jsonText.Trim();

            var root = JsonSerializer.Deserialize<JsonElement>(jsonText);

            // ═══════════════════════════════════════════════════════════════════
            // La API devuelve una estructura con array "paginas"
            // Los datos reales están en paginas[0]
            // ═══════════════════════════════════════════════════════════════════

            JsonElement doc;
            if (root.TryGetProperty("paginas", out var paginas) && 
                paginas.ValueKind == JsonValueKind.Array &&
                paginas.GetArrayLength() > 0)
            {
                // Extraer la primera página del array
                doc = paginas[0];
                _logger.LogDebug("[OCR] Estructura con páginas detectada. Usando página 0.");
            }
            else
            {
                // Formato antiguo sin páginas (retrocompatibilidad)
                doc = root;
                _logger.LogDebug("[OCR] Estructura plana detectada (sin páginas).");
            }

            var metadata = doc.TryGetProperty("metadata_lectura", out var meta)
                ? meta : default;

            var porcentaje = metadata.ValueKind != JsonValueKind.Undefined
                && metadata.TryGetProperty("porcentaje_lectura", out var pct)
                ? pct.GetDecimal() : 0m;

            var calidad = metadata.ValueKind != JsonValueKind.Undefined
                && metadata.TryGetProperty("calidad_lectura", out var cal)
                ? cal.GetString()?.Trim().ToLowerInvariant() ?? "baja"
                : "baja";

            _logger.LogInformation(
                "[OCR] Imagen {Id} | Calidad recibida: '{Calidad}' | Porcentaje: {Porcentaje}% | Umbral: {Umbral}%",
                idImagen, calidad, porcentaje, umbral);

            var camposIlegibles = new List<string>();
            if (metadata.ValueKind != JsonValueKind.Undefined
                && metadata.TryGetProperty("campos_ilegibles", out var campos)
                && campos.ValueKind == JsonValueKind.Array)
            {
                foreach (var campo in campos.EnumerateArray())
                {
                    var s = campo.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                        camposIlegibles.Add(s);
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            // REGLA DE LEGIBILIDAD: Basada ÚNICAMENTE en metadata_lectura del OCR
            // NO se aplica lógica adicional ni se modifican los valores del OCR
            // ═══════════════════════════════════════════════════════════════════

            // CALIDAD → LEGIBILIDAD:
            // - "alta" o "media" (o variantes como "media-alta", "media baja") → legible
            // - "baja" pura → ilegible (requiere transcripción manual)
            // Se usa Contains para tolerar variantes de redacción del LLM.
            var esLegible = calidad.Contains("alta") || calidad.Contains("media");

            // PORCENTAJE → CONFIANZA:
            // - Si es legible y porcentaje < umbral → requiere revisión adicional
            // - Si es legible y porcentaje >= umbral → aprobado para uso directo
            var esConfianzaBaja = esLegible && porcentaje < umbral;

            _logger.LogInformation(
                "[OCR] Decisión final basada en API OCR | Imagen {Id} | " +
                "Calidad: '{Calidad}' | Porcentaje: {Porcentaje}% | " +
                "EsLegible: {Legible} | EsConfianzaBaja: {ConfianzaBaja} | " +
                "Campos ilegibles: {Count}",
                idImagen, calidad, porcentaje, esLegible, esConfianzaBaja, camposIlegibles.Count);

            var paciente = doc.TryGetProperty("paciente", out var pac) ? pac : default;
            var medico   = doc.TryGetProperty("medico",   out var med) ? med : default;
            var consulta = doc.TryGetProperty("consulta", out var con) ? con : default;

            // Parsear medicamentos
            var medicamentos = new List<MedicamentoOcrDto>();
            if (doc.TryGetProperty("medicamentos", out var meds) &&
                meds.ValueKind == JsonValueKind.Array)
            {
                foreach (var m in meds.EnumerateArray())
                {
                    var medicamento = new MedicamentoOcrDto
                    {
                        Numero             = GetInt(m, "numero"),
                        NombreComercial    = GetStr(m, "nombre_comercial"),
                        SustanciaActiva    = GetStr(m, "sustancia_activa"),
                        Presentacion       = GetStr(m, "presentacion"),
                        Dosis              = GetStr(m, "dosis"),
                        CantidadTexto      = GetStr(m, "cantidad_texto"),
                        CantidadNumero     = GetInt(m, "cantidad_numero"),
                        UnidadCantidad     = GetStr(m, "unidad_cantidad"),
                        ViaAdministracion  = GetStr(m, "via_administracion"),
                        FrecuenciaTexto    = GetStr(m, "frecuencia_texto"),
                        FrecuenciaExpandida = GetStr(m, "frecuencia_expandida"),
                        DuracionTexto      = GetStr(m, "duracion_texto"),
                        DuracionDias       = GetInt(m, "duracion_dias"),
                        Indicaciones       = GetStr(m, "indicaciones"),
                        CodigoCIE10        = GetStr(m, "codigo_cie10"),
                        CodigoEAN          = GetStr(m, "codigo_ean")
                    };

                    // Solo agregar medicamentos que tengan al menos nombre
                    if (!string.IsNullOrWhiteSpace(medicamento.NombreComercial))
                    {
                        medicamentos.Add(medicamento);
                    }
                }
            }

            _logger.LogInformation(
                "[OCR] Medicamentos parseados: {Count}",
                idImagen, medicamentos.Count);

            var notasOcr = metadata.ValueKind != JsonValueKind.Undefined
                && metadata.TryGetProperty("notas", out var notas)
                ? notas.GetString() : null;

            // Construir notas adicionales si hay campos ilegibles
            var notasCompletas = notasOcr;
            if (camposIlegibles.Count > 0)
            {
                var camposTexto = string.Join(", ", camposIlegibles);
                notasCompletas = string.IsNullOrWhiteSpace(notasOcr)
                    ? $"Campos ilegibles: {camposTexto}"
                    : $"{notasOcr}. Campos ilegibles: {camposTexto}";
            }

            var resultado = new OcrResultadoDto
            {
                IdImagen           = idImagen,
                Exitoso            = true,
                EsLegible          = esLegible,
                EsConfianzaBaja    = esConfianzaBaja,
                ConfianzaPromedio  = porcentaje,
                CalidadLectura     = calidad,
                CamposIlegibles    = camposIlegibles,
                Notas              = notasCompletas,
                DuracionMs         = durationMs,

                AseguradoraDetectada = GetStr(doc, "aseguradora_detectada"),
                FormatoReceta        = GetStr(doc, "formato_receta"),

                NombrePaciente    = paciente.ValueKind != JsonValueKind.Undefined ? GetStr(paciente, "nombre_completo")    : null,
                ApellidoPaterno   = paciente.ValueKind != JsonValueKind.Undefined ? GetStr(paciente, "apellido_paterno")   : null,
                ApellidoMaterno   = paciente.ValueKind != JsonValueKind.Undefined ? GetStr(paciente, "apellido_materno")   : null,
                NombresPaciente   = paciente.ValueKind != JsonValueKind.Undefined ? GetStr(paciente, "nombres")            : null,
                Nomina            = paciente.ValueKind != JsonValueKind.Undefined ? GetStr(paciente, "nomina")             : null,
                Credencial        = paciente.ValueKind != JsonValueKind.Undefined ? GetStr(paciente, "credencial")         : null,
                NUR               = paciente.ValueKind != JsonValueKind.Undefined ? GetStr(paciente, "nur")                : null,
                NumeroAutorizacion = paciente.ValueKind != JsonValueKind.Undefined ? GetStr(paciente, "numero_autorizacion") : null,
                Elegibilidad      = paciente.ValueKind != JsonValueKind.Undefined ? GetStr(paciente, "elegibilidad")       : null,

                NombreMedico      = medico.ValueKind != JsonValueKind.Undefined ? GetStr(medico, "nombre_completo")    : null,
                CedulaMedico      = medico.ValueKind != JsonValueKind.Undefined ? GetStr(medico, "cedula_profesional")  : null,
                EspecialidadMedico = medico.ValueKind != JsonValueKind.Undefined ? GetStr(medico, "especialidad")       : null,
                InstitucionMedico = medico.ValueKind != JsonValueKind.Undefined ? GetStr(medico, "institucion")         : null,

                FechaConsulta    = consulta.ValueKind != JsonValueKind.Undefined ? GetStr(consulta, "fecha")              : null,
                DiagnosticoTexto = consulta.ValueKind != JsonValueKind.Undefined ? GetStr(consulta, "diagnostico_texto")  : null,
                CodigoCIE10      = consulta.ValueKind != JsonValueKind.Undefined ? GetStr(consulta, "codigo_cie10")       : null,

                Medicamentos = medicamentos
            };

            _logger.LogInformation(
                "[OCR] Resultado final para imagen {Id} | Legible: {Legible} | Confianza: {Confianza}% | " +
                "Medicamentos: {Meds} | Paciente: {Paciente} | Médico: {Medico}",
                idImagen, 
                resultado.EsLegible,
                resultado.ConfianzaPromedio,
                resultado.Medicamentos.Count,
                !string.IsNullOrWhiteSpace(resultado.NombrePaciente) ? "Sí" : "No",
                !string.IsNullOrWhiteSpace(resultado.NombreMedico) ? "Sí" : "No");

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parseando respuesta OCR para imagen {Id}. JSON recibido: {Json}", 
                idImagen, body.Length > 1000 ? body[..1000] + "..." : body);
            return OcrResultadoDto.Fallido($"Error parseando respuesta: {ex.Message}");
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string? GetStr(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    private static int? GetInt(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetInt32() : null;
}
