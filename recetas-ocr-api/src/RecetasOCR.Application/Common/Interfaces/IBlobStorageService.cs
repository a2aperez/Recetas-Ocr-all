namespace RecetasOCR.Application.Common.Interfaces;

/// <summary>
/// Abstracción sobre Azure Blob Storage.
/// REGLA: SubirRawAsync se llama SIEMPRE para toda imagen — UrlBlobRaw es NOT NULL.
/// SubirOcrAsync     → solo si la imagen es legible   (recetas-ocr).
/// SubirIlegibleAsync→ solo si la imagen es ilegible  (recetas-ilegibles).
/// Los blobs ilegibles NUNCA se eliminan.
/// </summary>
public interface IBlobStorageService
{
    Task<string> SubirRawAsync(Stream imagen, string nombreArchivo, CancellationToken ct = default);
    Task<string> SubirOcrAsync(Stream imagen, string nombreArchivo, CancellationToken ct = default);
    Task<string> SubirIlegibleAsync(Stream imagen, string nombreArchivo, CancellationToken ct = default);
    Task<string> SubirCfdiXmlAsync(Stream xml, string nombreArchivo, CancellationToken ct = default);
    Task<string> SubirCfdiPdfAsync(Stream pdf, string nombreArchivo, CancellationToken ct = default);
    Task<Stream> DescargarAsync(string urlBlob, CancellationToken ct = default);
    // Los blobs NUNCA se eliminan desde la aplicación — no hay método EliminarAsync
}
