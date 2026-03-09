namespace RecetasOCR.Application.DTOs.Catalogos;

public record ModuloDto(
    int     Id,
    string  Clave,
    string  Nombre,
    string? Descripcion
);
