using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Ocr;
using RecetasOCR.Application.DTOs.Paginacion;
using RecetasOCR.Application.Features.Ocr;

namespace RecetasOCR.API.Controllers;

[ApiController]
[Route("api/ocr")]
[Authorize]
public class OcrController(IMediator mediator) : ControllerBase
{
    [HttpGet("imagen/{idImagen:guid}/estado")]
    [Authorize("IMAGENES.VER")]
    public async Task<ActionResult<ApiResponse<EstadoOcrDto>>> GetEstado(Guid idImagen, CancellationToken ct)
    {
        var result = await mediator.Send(new GetEstadoOcrQuery(idImagen), ct);
        return Ok(ApiResponse<EstadoOcrDto>.Ok(result));
    }

    [HttpPost("imagen/{idImagen:guid}/reprocesar")]
    [Authorize("REVISION.APROBAR.escribir")]
    public async Task<ActionResult<ApiResponse<EstadoOcrDto>>> Reprocesar(
        Guid idImagen, [FromBody] ReprocesarImagenCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command with { IdImagen = idImagen }, ct);
        return Ok(ApiResponse<EstadoOcrDto>.Ok(result));
    }

    [HttpGet("imagen/{idImagen:guid}/resultado")]
    [Authorize("REVISION.VER")]
    public async Task<ActionResult<ApiResponse<ResultadoOcrDetalleDto>>> GetResultado(Guid idImagen, CancellationToken ct)
    {
        var result = await mediator.Send(new GetResultadoOcrQuery(idImagen), ct);
        return Ok(ApiResponse<ResultadoOcrDetalleDto>.Ok(result));
    }

    [HttpGet("cola")]
    [Authorize("CONFIG.EDITAR")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<ColaOcrItemDto>>>> GetCola(
        [FromQuery] string? estadoCola, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetColaOcrQuery(estadoCola, page, pageSize), ct);
        return Ok(ApiResponse<PagedResultDto<ColaOcrItemDto>>.Ok(result));
    }
}
