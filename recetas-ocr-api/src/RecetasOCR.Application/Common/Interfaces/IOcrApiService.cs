using RecetasOCR.Application.DTOs;

namespace RecetasOCR.Application.Common.Interfaces;

/// <summary>
/// Cliente para la API externa de OCR configurada en cfg.ConfiguracionesOCR.
/// Sin librerías OCR locales (sin Tesseract).
/// Usa Polly para reintentos con backoff exponencial.
/// Registra cada llamada en ocr.ResultadosOCR.
/// </summary>
public interface IOcrApiService
{
    /// <summary>
    /// Envía la imagen al proveedor OCR activo y retorna el resultado.
    /// Si la confianza es menor al umbral (cfg.Parametros[OCR_CONFIANZA_MINIMA])
    /// el resultado es válido pero con EsConfianzaBaja=true — NO lanza excepción.
    /// </summary>
    Task<OcrResultadoDto> ProcesarImagenAsync(
        string urlBlobRaw,
        Guid idImagen,
        CancellationToken ct = default);
}
