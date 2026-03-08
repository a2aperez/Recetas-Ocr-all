namespace RecetasOCR.Application.DTOs;

public record OcrResultadoDto(
    bool Exitoso,
    bool EsLegible,
    bool EsConfianzaBaja,
    decimal ConfianzaPromedio,
    string? TextoCompleto,
    string? MotivoBajaCalidad,
    string? ResponseJsonCompleto,
    decimal CostoEstimadoUsd,
    string? MensajeError
);
