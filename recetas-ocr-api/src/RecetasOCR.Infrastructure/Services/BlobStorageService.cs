using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Common;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Infrastructure.Services;

/// <summary>
/// Implementación de IBlobStorageService usando Azure.Storage.Blobs.
///
/// Reglas de negocio:
///   - SubirRawAsync  → siempre la primera operación para toda imagen.
///   - SubirIlegibleAsync → copia lógica al contenedor recetas-ilegibles; el raw NO se elimina.
///   - No existe EliminarAsync: los blobs NUNCA se eliminan desde la aplicación.
///   - Nombre de blob: {newGuid}/{nombreArchivo}  (incluye carpeta por guid para dispersión).
///   - Retorna BlobClient.Uri.ToString() para persistir en la BD.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        var connectionString = configuration["AzureBlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Falta configuración AzureBlobStorage:ConnectionString.");

        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }

    // ─── Upload helpers ────────────────────────────────────────────────────────

    public Task<string> SubirRawAsync(Stream imagen, string nombreArchivo, CancellationToken ct = default)
        => SubirStreamAsync(imagen, nombreArchivo, Constantes.BlobContainers.RAW, "SubirRawAsync", ct);

    public Task<string> SubirOcrAsync(Stream imagen, string nombreArchivo, CancellationToken ct = default)
        => SubirStreamAsync(imagen, nombreArchivo, Constantes.BlobContainers.OCR, "SubirOcrAsync", ct);

    public Task<string> SubirIlegibleAsync(Stream imagen, string nombreArchivo, CancellationToken ct = default)
        => SubirStreamAsync(imagen, nombreArchivo, Constantes.BlobContainers.ILEGIBLE, "SubirIlegibleAsync", ct);

    public Task<string> SubirCfdiXmlAsync(Stream xml, string nombreArchivo, CancellationToken ct = default)
        => SubirStreamAsync(xml, nombreArchivo, Constantes.BlobContainers.CFDI_XML, "SubirCfdiXmlAsync", ct);

    public Task<string> SubirCfdiPdfAsync(Stream pdf, string nombreArchivo, CancellationToken ct = default)
        => SubirStreamAsync(pdf, nombreArchivo, Constantes.BlobContainers.CFDI_PDF, "SubirCfdiPdfAsync", ct);

    // ─── Download (para Worker → API OCR) ─────────────────────────────────────

    /// <summary>
    /// Descarga un blob à partir de su URL absoluta.
    /// El worker lee el raw, lo convierte a Base64 y lo envía a la API OCR.
    /// El stream retornado no está buscado (position=0 lo garantiza BlobDownloadInfo).
    /// </summary>
    public async Task<Stream> DescargarAsync(string urlBlob, CancellationToken ct = default)
    {
        const string operacion = "DescargarAsync";
        try
        {
            // Usar BlobUriBuilder para parsear correctamente la URL de Azure Blob
            var blobUriBuilder = new BlobUriBuilder(new Uri(urlBlob));
            var contenedor = blobUriBuilder.BlobContainerName;
            var blobName = blobUriBuilder.BlobName;

            if (string.IsNullOrWhiteSpace(contenedor) || string.IsNullOrWhiteSpace(blobName))
            {
                _logger.LogError(
                    "[BlobStorage] URL de blob inválida: {Url} | Contenedor: {Container} | BlobName: {BlobName}",
                    urlBlob, contenedor ?? "(null)", blobName ?? "(null)");

                throw new InvalidOperationException(
                    $"URL de blob inválida (no se pudo extraer contenedor o blob name): {urlBlob}");
            }

            _logger.LogDebug(
                "[BlobStorage] Descargando blob | Contenedor: {Container} | BlobName: {BlobName} | URL: {Url}",
                contenedor, blobName, urlBlob);

            // Usar _blobServiceClient para heredar la autenticación
            var containerClient = _blobServiceClient.GetBlobContainerClient(contenedor);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Verificar si el blob existe antes de intentar descargarlo
            var existe = await blobClient.ExistsAsync(ct);
            if (!existe.Value)
            {
                _logger.LogWarning(
                    "[BlobStorage] Blob no encontrado | Contenedor: {Container} | BlobName: {BlobName} | URL: {Url}",
                    contenedor, blobName, urlBlob);

                throw new BlobStorageException(
                    operacion,
                    contenedor,
                    new InvalidOperationException(
                        $"El blob '{blobName}' no existe en el contenedor '{contenedor}'. " +
                        $"Verifique que el archivo se haya subido correctamente. URL: {urlBlob}"));
            }

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct);

            _logger.LogDebug(
                "[BlobStorage] Blob descargado exitosamente | Contenedor: {Container} | BlobName: {BlobName}",
                contenedor, blobName);

            return response.Value.Content;
        }
        catch (Exception ex) when (ex is not BlobStorageException)
        {
            // Extraemos el nombre de contenedor de la URL para el mensaje de error.
            var contenedor = ExtraerContenedor(urlBlob);

            _logger.LogError(ex,
                "[BlobStorage] Error al descargar blob | Contenedor: {Container} | URL: {Url}",
                contenedor, urlBlob);

            throw new BlobStorageException(operacion, contenedor, ex);
        }
    }

    // ─── Implementación común ──────────────────────────────────────────────────

    private async Task<string> SubirStreamAsync(
        Stream stream,
        string nombreArchivo,
        string contenedor,
        string operacion,
        CancellationToken ct)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(contenedor);

            // Crea el contenedor si no existe (útil en Development/Azure).
            await containerClient.CreateIfNotExistsAsync(
                PublicAccessType.None, cancellationToken: ct);

            // Guid como carpeta de dispersión evita hot partitions en Storage.
            var blobName = $"{Guid.NewGuid()}/{nombreArchivo}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Garantiza que el stream esté al inicio antes de subir.
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);

            await blobClient.UploadAsync(stream, overwrite: false, ct);

            return blobClient.Uri.ToString();
        }
        catch (Exception ex) when (ex is not BlobStorageException)
        {
            throw new BlobStorageException(operacion, contenedor, ex);
        }
    }

    // ─── Utilidades ────────────────────────────────────────────────────────────

    /// <summary>
    /// Extrae el nombre del contenedor desde una URL de Azure Blob Storage.
    /// Formato: https://{account}.blob.core.windows.net/{container}/{blobName}
    /// </summary>
    private static string ExtraerContenedor(string urlBlob)
    {
        try
        {
            var uri = new Uri(urlBlob);
            // uri.Segments: ["/" , "container/", "guid/", "file.jpg"]
            return uri.Segments.Length > 1
                ? uri.Segments[1].TrimEnd('/')
                : "desconocido";
        }
        catch
        {
            return "desconocido";
        }
    }
}
