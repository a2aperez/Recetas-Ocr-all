namespace RecetasOCR.Application.DTOs.Medicamentos;

/// <summary>
/// Representación de med.MedicamentosReceta para el cliente.
/// Incluye los datos clínicos del medicamento prescrito en la receta,
/// tanto los extraídos por OCR como los corregidos por el revisor.
/// </summary>
public record MedicamentoRecetaDto(
    Guid     Id,
    Guid     IdImagen,
    Guid     IdGrupo,
    int?     IdMedicamentoCatalogo,
    int      NumeroPrescripcion,
    string?  CodigoCie10,
    string?  DescripcionCie10,
    string?  NombreComercial,
    string?  NombreGenerico,
    string?  Presentacion,
    string?  Concentracion,
    string?  FormaFarmaceutica,
    string?  ViaAdministracion,
    string?  Dosis,
    string?  FrecuenciaTexto,
    string?  FrecuenciaExpandida,
    string?  DuracionTexto,
    int?     DuracionDias,
    string?  IndicacionesCompletas,
    string?  NumeroAutorizacion,
    bool     FueExtraido,
    decimal? ConfianzaExtraccion,
    bool     ValidadoPorRevisor,
    string?  ModificadoPor,
    DateTime FechaModificacion
);
