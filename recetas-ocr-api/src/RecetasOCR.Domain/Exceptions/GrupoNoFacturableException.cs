namespace RecetasOCR.Domain.Exceptions;

/// <summary>
/// Se lanza cuando se intenta facturar un grupo que no está en
/// estado REVISADO_COMPLETO. Regla de negocio inamovible:
/// solo grupos REVISADO_COMPLETO avanzan a CFDI 4.0.
/// </summary>
public class GrupoNoFacturableException : Exception
{
    public Guid IdGrupo { get; }
    public string EstadoActual { get; }

    public GrupoNoFacturableException(Guid idGrupo, string estadoActual)
        : base(
            $"El grupo {idGrupo} está en estado '{estadoActual}'. " +
            $"Solo grupos en REVISADO_COMPLETO pueden facturarse.")
    {
        IdGrupo = idGrupo;
        EstadoActual = estadoActual;
    }
}