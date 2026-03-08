namespace RecetasOCR.Domain.Exceptions;

/// <summary>
/// Se lanza cuando se intenta procesar una imagen que fue evaluada
/// como ilegible por la API OCR externa.
/// La imagen ilegible NUNCA se elimina — permanece en blob recetas-ilegibles.
/// </summary>
public class ImagenIlegibleException : Exception
{
    public Guid IdImagen { get; }
    public string Motivo { get; }

    public ImagenIlegibleException(Guid idImagen, string motivo)
        : base($"La imagen {idImagen} es ilegible: {motivo}. Requiere captura manual.")
    {
        IdImagen = idImagen;
        Motivo = motivo;
    }
}