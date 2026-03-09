namespace RecetasOCR.Application.DTOs.Catalogos;

public record ConfiguracionOcrDto(
    int     Id,
    string  Nombre,
    string  Proveedor,
    string  UrlBase,
    string? ApiKeyMasked,       // primeros 8 chars + "****"
    string? Modelo,
    string? Version,
    int     TimeoutSegundos,
    int     MaxReintentos,
    decimal CostoPorImagenUsd,
    bool    EsPrincipal,
    bool    Activo,
    string? ConfigJson,
    DateTime FechaActualizacion
);
