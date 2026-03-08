namespace RecetasOCR.Application.DTOs.Facturacion;

public record CfdiDto(
    Guid      Id,
    Guid      IdPreFactura,
    string    UUID,
    string    Version,
    decimal   Total,
    string    Estado,
    string    UrlXml,
    string?   UrlPdf,
    string?   NoCertificadoSAT,
    string?   SelloSAT,
    DateTime  FechaTimbrado,
    DateTime  FechaCreacion
);
