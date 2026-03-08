namespace RecetasOCR.Domain.Enums;

/// <summary>
/// Pasos del procesamiento registrados en aud.LogProcesamiento.
/// Columna Paso NVARCHAR(80).
/// Cada valor corresponde a un punto de control del OcrWorkerService
/// y de los handlers de la capa Application.
/// </summary>
public enum PasoLog
{
    /// <summary>Petición enviada a la API OCR externa.</summary>
    OcrPeticion = 1,

    /// <summary>Respuesta recibida de la API OCR externa.</summary>
    OcrRespuesta,

    /// <summary>Error al llamar a la API OCR externa.</summary>
    OcrError,

    /// <summary>Extracción de campos estructurados desde el texto OCR.</summary>
    Extraccion,

    /// <summary>Evaluación de legibilidad de la imagen.</summary>
    Legibilidad,

    /// <summary>Operación sobre ocr.ColaProcesamiento (encolado / bloqueo / liberación).</summary>
    Cola,

    /// <summary>Cambio de estado en cat.EstadosImagen o cat.EstadosGrupo.</summary>
    EstadoCambio,

    /// <summary>Error de comunicación con la API externa (timeout, 5xx, red).</summary>
    ErrorApi
}
