using RecetasOCR.Application.DTOs;

namespace RecetasOCR.Application.Common.Interfaces;

/// <summary>
/// Cliente para la API externa de OCR de Nadro.
/// Usa los bytes de la imagen directamente (sin re-descarga del blob).
/// No accede a la base de datos — solo llama a la API y retorna el resultado parseado.
/// </summary>
public interface IOcrApiService
{
    /// <summary>
    /// Envía la imagen al proveedor OCR activo y retorna el resultado.
    /// Si la confianza es menor al umbral (cfg.Parametros[OCR_UMBRAL_CONFIANZA])
    /// el resultado es válido pero con EsConfianzaBaja=true — NO lanza excepción.
    /// </summary>
    Task<OcrResultadoDto> ProcesarImagenAsync(
        string urlBlobRaw,
        Guid   idImagen,
        byte[] archivoBytes,
        string mimeType,
        CancellationToken ct = default);
}
