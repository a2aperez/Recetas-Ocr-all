namespace RecetasOCR.Application.DTOs.Catalogos;

public record MedicamentoCatalogoDto(
    int     Id,
    string  NombreComercial,
    string? SustanciaActiva,
    string? Presentacion,
    string? CodigoEAN,
    bool    Activo
);
