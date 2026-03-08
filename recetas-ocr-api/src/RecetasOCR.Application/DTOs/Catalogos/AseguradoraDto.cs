namespace RecetasOCR.Application.DTOs.Catalogos;

public record AseguradoraDto(
    int     Id,
    string  Nombre,
    string  Clave,
    string? RazonSocial,
    string? RFC,
    bool    Activo
);
