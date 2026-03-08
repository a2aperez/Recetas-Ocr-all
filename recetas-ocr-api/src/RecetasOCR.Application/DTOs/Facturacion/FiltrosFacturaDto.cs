namespace RecetasOCR.Application.DTOs.Facturacion;

public record FiltrosFacturaDto(
    int?      IdAseguradora  = null,
    DateTime? FechaDesde     = null,
    DateTime? FechaHasta     = null,
    string?   RFC            = null,
    string?   Estado         = null,
    int       Page           = 1,
    int       PageSize       = 20
);
