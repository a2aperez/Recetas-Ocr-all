namespace RecetasOCR.Application.DTOs.Facturacion;

public record PacResultadoDto(
    bool    Exitoso,
    string? UUID,
    string? XmlTimbrado,
    string? QrBase64,
    string? CadenaOriginal,
    string? NoCertificadoSAT,
    string? SelloSAT,
    string? MensajeError,
    string? CodigoError
);
