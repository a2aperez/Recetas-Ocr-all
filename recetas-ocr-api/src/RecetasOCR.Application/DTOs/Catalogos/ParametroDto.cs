namespace RecetasOCR.Application.DTOs.Catalogos;

public record ParametroDto(
    int     Id,
    string  Clave,
    string  Valor,          // "***" cuando EsSecreto = true
    string? Descripcion,
    string  Tipo,
    bool    EsSecreto
);
