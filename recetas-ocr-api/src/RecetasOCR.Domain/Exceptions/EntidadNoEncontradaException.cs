namespace RecetasOCR.Domain.Exceptions;

/// <summary>
/// Se lanza cuando una entidad solicitada no existe en la base de datos.
/// </summary>
public class EntidadNoEncontradaException : Exception
{
    public string Entidad { get; }
    public object Id { get; }

    public EntidadNoEncontradaException(string entidad, object id)
        : base($"{entidad} con id '{id}' no fue encontrado.")
    {
        Entidad = entidad;
        Id = id;
    }
}