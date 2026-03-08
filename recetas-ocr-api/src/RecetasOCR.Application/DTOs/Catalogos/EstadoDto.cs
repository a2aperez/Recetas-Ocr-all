namespace RecetasOCR.Application.DTOs.Catalogos;

public record EstadoDto(
    int     Id,
    string  Clave,
    string  Nombre,
    string? Descripcion
);
