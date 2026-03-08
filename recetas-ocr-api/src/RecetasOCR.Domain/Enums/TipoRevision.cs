namespace RecetasOCR.Domain.Enums;

/// <summary>
/// Tipo de revisión humana registrada en rev.AsignacionesRevision
/// y rev.RevisionesHumanas.
/// </summary>
public enum TipoRevision
{
    CapturaManual = 1,
    CorreccionOcr,
    Validacion,
    DatosFiscales
}
