namespace RecetasOCR.Domain.Exceptions;

/// <summary>
/// Se lanza cuando se intenta realizar una operación sobre una entidad
/// que no se encuentra en un estado válido para dicha operación.
/// Aplica a rec.GruposReceta y rec.Imagenes.
/// </summary>
public class EstadoInvalidoException : Exception
{
    public string Entidad { get; }
    public string EstadoActual { get; }
    public string[] EstadosPermitidos { get; }

    public EstadoInvalidoException(string entidad, string estadoActual, string[] estadosPermitidos)
        : base(
            $"{entidad} está en estado '{estadoActual}'. " +
            $"Solo se permite esta operación desde: {string.Join(", ", estadosPermitidos)}.")
    {
        Entidad = entidad;
        EstadoActual = estadoActual;
        EstadosPermitidos = estadosPermitidos;
    }
}
