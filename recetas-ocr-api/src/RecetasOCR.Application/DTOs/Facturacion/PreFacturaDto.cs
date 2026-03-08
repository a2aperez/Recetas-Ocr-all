namespace RecetasOCR.Application.DTOs.Facturacion;

public record PreFacturaDto(
    Guid                      Id,
    Guid                      IdGrupo,
    string                    Estado,
    string                    RFC,
    string                    NombreFiscal,
    string                    UsoCFDI,
    string                    MetodoPago,
    string                    FormaPago,
    decimal                   Subtotal,
    decimal                   IVA,
    decimal                   Total,
    List<ConceptoFacturaDto>  Conceptos,
    DateTime                  FechaCreacion,
    DateTime                  FechaModificacion
);
