namespace RecetasOCR.Application.DTOs.GruposReceta;

/// <summary>
/// Campos principales de rec.GruposReceta para listados paginados.
/// Usado en GET /api/grupos-receta.
/// Alineado con GrupoReceta del frontend (grupo-receta.types.ts).
/// </summary>
public record GrupoRecetaDto(
    Guid      Id,
    string?   FolioBase,
    Guid?     IdCliente,
    int       IdAseguradora,
    string?   NombreAseguradora,
    string?   Nur,
    string?   NombrePaciente,
    string?   ApellidoPaterno,
    string?   ApellidoMaterno,
    string?   NombreMedico,
    string?   CedulaMedico,
    string?   EspecialidadTexto,
    string?   CodigoCie10,
    string?   DescripcionDiagnostico,
    DateOnly? FechaConsulta,
    int       TotalImagenes,
    int       TotalMedicamentos,
    string    EstadoGrupo,
    DateTime  FechaCreacion,
    DateTime  FechaActualizacion,
    string?   ModificadoPor,
    DateTime  FechaModificacion
);
