namespace RecetasOCR.Application.DTOs.GruposReceta;

/// <summary>
/// Parámetros de filtrado y paginación para GET /api/grupos-receta.
/// Todos los filtros son opcionales excepto Page y PageSize.
/// Busqueda aplica sobre FolioBase, NombrePaciente y NombreMedico.
/// PageSize máximo permitido: 100 (validado con FluentValidation).
/// </summary>
public record FiltrosGrupoDto(
    int?      IdAseguradora = null,
    string?   EstadoGrupo   = null,
    DateTime? FechaDesde    = null,
    DateTime? FechaHasta    = null,
    string?   Busqueda      = null,
    int       Page          = 1,
    int       PageSize      = 20
);
