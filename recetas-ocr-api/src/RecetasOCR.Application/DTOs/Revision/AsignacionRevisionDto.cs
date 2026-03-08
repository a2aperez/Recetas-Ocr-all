namespace RecetasOCR.Application.DTOs.Revision;

/// <summary>
/// Representación de rev.AsignacionesRevision para el cliente.
/// Usado en GET /api/revision/cola y GET /api/revision/{id}.
/// </summary>
public record AsignacionRevisionDto(
    Guid      Id,
    Guid      IdImagen,
    Guid      IdGrupo,
    Guid      IdUsuarioAsignado,
    string    TipoRevision,
    DateTime  FechaAsignacion,
    DateTime? FechaLimite,
    DateTime? FechaInicio,
    DateTime? FechaTermino,
    string    Estado,
    int       Prioridad,
    string?   Notas,
    string?   ModificadoPor,
    DateTime  FechaModificacion
);
