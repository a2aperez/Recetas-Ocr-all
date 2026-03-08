namespace RecetasOCR.Domain.Exceptions;

/// <summary>
/// Se lanza cuando falla una operación contra Azure Blob Storage.
/// Contenedores posibles: recetas-raw, recetas-ocr, recetas-ilegibles, cfdi-xml, cfdi-pdf.
/// El contenedor y la operación se incluyen para facilitar el diagnóstico en logs.
/// </summary>
public class BlobStorageException : Exception
{
    public string Operacion { get; }
    public string Contenedor { get; }

    public BlobStorageException(string operacion, string contenedor, Exception inner)
        : base(
            $"Error en operación '{operacion}' sobre el contenedor '{contenedor}': {inner.Message}",
            inner)
    {
        Operacion = operacion;
        Contenedor = contenedor;
    }
}