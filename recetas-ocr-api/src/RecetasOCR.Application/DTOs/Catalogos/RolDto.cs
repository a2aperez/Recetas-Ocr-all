namespace RecetasOCR.Application.DTOs.Catalogos;

public record RolDto(
    int     Id,
    string  Clave,
    string  Nombre,
    string? Descripcion
);
