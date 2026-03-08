using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Imagenes;
using RecetasOCR.Application.Features.Imagenes;

namespace RecetasOCR.API.Controllers;

[ApiController]
[Route("api/imagenes")]
[Authorize]
public class ImagenesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize("IMAGENES.SUBIR.escribir")]
    public async Task<ActionResult<ApiResponse<Guid>>> Subir(
        [FromForm] SubirImagenRequest r, CancellationToken ct)
    {
        var cmd = new SubirImagenCommand(r.IdGrupo, r.Archivo.OpenReadStream(),
            r.Archivo.FileName, r.Archivo.ContentType, r.Archivo.Length, r.OrigenImagen);
        return Ok(ApiResponse<Guid>.Ok(await mediator.Send(cmd, ct)));
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
}

public record SubirImagenRequest(Guid IdGrupo, IFormFile Archivo, string OrigenImagen);
