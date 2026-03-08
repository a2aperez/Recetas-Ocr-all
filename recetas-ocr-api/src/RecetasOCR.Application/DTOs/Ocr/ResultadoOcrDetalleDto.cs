namespace RecetasOCR.Application.DTOs.Ocr;

/// <summary>
/// Detalle completo del resultado OCR + extracción estructurada.
/// Incluye todos los campos de EstadoOcrDto más el payload crudo y campos semánticos.
/// </summary>
public record ResultadoOcrDetalleDto(
    // ── EstadoOcrDto fields ─────────────────────────────────────────────────
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
    bool?     Exitoso,
    // ── Resultado OCR extendido ─────────────────────────────────────────────
    string?   ResponseJsonCompleto,
    string?   TextoCompleto,
    string?   CamposFaltantes,
    string?   AseguradoraDetectada,
    string?   FormatoDetectado,
    int?      TokensEntrada,
    int?      TokensSalida,
    decimal?  CostoEstimadoUsd
);
