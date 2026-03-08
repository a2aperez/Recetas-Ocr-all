namespace RecetasOCR.Application.DTOs.Facturacion;

public record ConceptoFacturaDto(
    Guid     Id,
    int      NumeroPrescripcion,
    string   Descripcion,
    string?  ClaveSAT,
    decimal  Cantidad,
    decimal  PrecioUnitario,
    decimal  Importe,
    decimal  IVA
);
