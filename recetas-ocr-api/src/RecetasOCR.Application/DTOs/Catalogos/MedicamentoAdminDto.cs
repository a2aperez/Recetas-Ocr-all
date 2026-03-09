namespace RecetasOCR.Application.DTOs.Catalogos;

/// <summary>
/// DTO completo para administración de catálogo de medicamentos.
/// </summary>
public record MedicamentoAdminDto(
    int     Id,
    string  NombreComercial,
    string? SustanciaActiva,
    string? Presentacion,
    string? Concentracion,
    string? ClaveSat,
    bool    Activo
);
