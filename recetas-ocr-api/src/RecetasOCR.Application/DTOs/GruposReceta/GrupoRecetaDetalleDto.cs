using RecetasOCR.Application.DTOs.Imagenes;
using RecetasOCR.Application.DTOs.Medicamentos;

namespace RecetasOCR.Application.DTOs.GruposReceta;

/// <summary>
/// Detalle completo de un grupo de receta.
/// Usado en GET /api/grupos-receta/{id}.
/// Incluye todas las imágenes y medicamentos del grupo.
/// </summary>
public record GrupoRecetaDetalleDto(
    Guid                       Id,
    string?                    FolioBase,
    Guid?                      IdCliente,
    int                        IdAseguradora,
    string?                    NombreAseguradora,
    string?                    Nur,
    string?                    NombrePaciente,
    string?                    ApellidoPaterno,
    string?                    ApellidoMaterno,
    string?                    NombreMedico,
    string?                    CedulaMedico,
    string?                    EspecialidadTexto,
    string?                    CodigoCie10,
    string?                    DescripcionDiagnostico,
    DateOnly?                  FechaConsulta,
    int                        TotalImagenes,
    int                        TotalMedicamentos,
    string                     EstadoGrupo,
    DateTime                   FechaCreacion,
    DateTime                   FechaActualizacion,
    string?                    ModificadoPor,
    DateTime                   FechaModificacion,
    List<ImagenDto>            Imagenes,
    List<MedicamentoRecetaDto> Medicamentos
);
