using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Imagenes;
using RecetasOCR.Application.Features.Imagenes;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.API.Controllers;

[ApiController]
[Route("api/imagenes")]
[Authorize]
public class ImagenesController(IMediator mediator, IBlobStorageService blob) : ControllerBase
{
    [HttpPost]
    [Authorize("IMAGENES.SUBIR.escribir")]
    public async Task<ActionResult<ApiResponse<ImagenDto>>> Subir(
        [FromForm] SubirImagenRequest r, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await r.Archivo.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        var cmd = new SubirImagenCommand(
            r.IdGrupo, r.Archivo.FileName, r.Archivo.ContentType,
            r.Archivo.Length, bytes, r.OrigenImagen);
        return Ok(ApiResponse<ImagenDto>.Ok(await mediator.Send(cmd, ct)));
    }

    [HttpGet("grupo/{idGrupo:guid}")]
    [Authorize("IMAGENES.VER")]
    public async Task<ActionResult<ApiResponse<List<ImagenDto>>>> GetPorGrupo(
        Guid idGrupo, CancellationToken ct)
    {
        var result = await mediator.Send(new GetImagenesPorGrupoQuery(idGrupo), ct);
        return Ok(ApiResponse<List<ImagenDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize("IMAGENES.VER")]
    public async Task<ActionResult<ApiResponse<ImagenDto>>> GetById(
        Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetImagenByIdQuery(id), ct);
        return Ok(ApiResponse<ImagenDto>.Ok(result));
    }

    [HttpGet("{id:guid}/ocr-estado")]
    [Authorize("IMAGENES.VER")]
    public async Task<ActionResult<ApiResponse<ImagenEstadoOcrDto>>> GetEstadoOcr(
        Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetEstadoOcrQuery(id), ct);
        return Ok(ApiResponse<ImagenEstadoOcrDto>.Ok(result));
    }

    /// <summary>Descarga proxy del blob raw (privado) — permite cargar la imagen en el browser con auth JWT.</summary>
    [HttpGet("{id:guid}/raw")]
    [Authorize("IMAGENES.VER")]
    public async Task<IActionResult> GetRaw(Guid id, CancellationToken ct)
    {
        try
        {
            var imagen = await mediator.Send(new GetImagenByIdQuery(id), ct);

            if (string.IsNullOrWhiteSpace(imagen.UrlBlobRaw))
                return NotFound(new { error = "La imagen no tiene un blob asociado." });

            var stream = await blob.DescargarAsync(imagen.UrlBlobRaw, ct);
            return File(stream, DetectarContentType(imagen.NombreArchivo));
        }
        catch (BlobStorageException ex) when (ex.InnerException?.Message?.Contains("no existe") == true)
        {
            return NotFound(new 
            { 
                error = "El archivo de imagen no se encontró en el almacenamiento.",
                detail = "Es posible que la imagen no se haya subido correctamente o se haya eliminado.",
                imageId = id
            });
        }
    }

    /// <summary>Descarga proxy del blob OCR anotado (privado).</summary>
    [HttpGet("{id:guid}/ocr")]
    [Authorize("IMAGENES.VER")]
    public async Task<IActionResult> GetOcrBlob(Guid id, CancellationToken ct)
    {
        try
        {
            var imagen = await mediator.Send(new GetImagenByIdQuery(id), ct);

            if (imagen.UrlBlobOcr is null)
                return NotFound(new { error = "Esta imagen no tiene blob OCR procesado." });

            var stream = await blob.DescargarAsync(imagen.UrlBlobOcr, ct);
            return File(stream, DetectarContentType(imagen.NombreArchivo));
        }
        catch (BlobStorageException ex) when (ex.InnerException?.Message?.Contains("no existe") == true)
        {
            return NotFound(new 
            { 
                error = "El archivo OCR no se encontró en el almacenamiento.",
                detail = "Es posible que el archivo OCR no se haya generado correctamente.",
                imageId = id
            });
        }
    }

    /// <summary>
    /// [DEBUG] Obtiene información de diagnóstico sobre las URLs de blob de una imagen.
    /// Útil para depurar problemas de blobs no encontrados.
    /// </summary>
    [HttpGet("{id:guid}/debug-urls")]
    [Authorize("IMAGENES.VER")]
    public async Task<ActionResult<object>> DebugUrls(Guid id, CancellationToken ct)
    {
        var imagen = await mediator.Send(new GetImagenByIdQuery(id), ct);

        return Ok(new
        {
            IdImagen = imagen.Id,
            NombreArchivo = imagen.NombreArchivo,
            UrlBlobRaw = imagen.UrlBlobRaw,
            UrlBlobOcr = imagen.UrlBlobOcr,
            UrlBlobIlegible = imagen.UrlBlobIlegible,
            EstadoImagen = imagen.EstadoImagen,
            FechaSubida = imagen.FechaSubida,
            ParsedInfo = imagen.UrlBlobRaw != null ? ParseBlobUrl(imagen.UrlBlobRaw) : null
        });
    }

    private static object ParseBlobUrl(string url)
    {
        try
        {
            var blobUriBuilder = new Azure.Storage.Blobs.BlobUriBuilder(new Uri(url));

            return new
            {
                UrlCompleta = url,
                AccountName = blobUriBuilder.AccountName,
                Contenedor = blobUriBuilder.BlobContainerName,
                BlobName = blobUriBuilder.BlobName,
                Host = blobUriBuilder.Host,
                Sas = blobUriBuilder.Sas != null ? "Presente" : "No presente"
            };
        }
        catch (Exception ex)
        {
            return new { Error = ex.Message, Url = url };
        }
    }

    private static string DetectarContentType(string nombreArchivo) =>
        Path.GetExtension(nombreArchivo).ToLowerInvariant() switch
        {
            ".png"  => "image/png",
            ".gif"  => "image/gif",
            ".webp" => "image/webp",
            ".bmp"  => "image/bmp",
            _       => "image/jpeg",
        };
}

public record SubirImagenRequest(Guid IdGrupo, IFormFile Archivo, string OrigenImagen);
