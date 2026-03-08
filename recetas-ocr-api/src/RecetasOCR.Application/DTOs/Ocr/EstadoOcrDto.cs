namespace RecetasOCR.Application.DTOs.Ocr;

/// <summary>
/// Estado OCR combinado de una imagen: estado EN rec.Imagenes +
/// registro más reciente en ocr.ColaProcesamiento + resultados de ocr.ResultadosOCR.
/// </summary>
public record EstadoOcrDto(
    Guid      IdImagen,
    string    EstadoImagen,
    string?   EstadoCola,
    int?      Intentos,
    int?      MaxIntentos,
    bool?     Bloqueado,
    DateTime? FechaEncolado,
    DateTime? FechaInicioProceso,
    DateTime? FechaFinProceso,
    decimal?  ConfianzaPromedio,
    bool?     EsLegible,
    string?   MotivoBajaCalidad,
    string?   ProveedorOcr,
    string?   ModeloUsado,
    int?      DuracionMs,
    bool?     Exitoso
);
