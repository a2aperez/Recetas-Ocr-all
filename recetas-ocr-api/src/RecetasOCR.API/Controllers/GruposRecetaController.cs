using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.GruposReceta;
using RecetasOCR.Application.DTOs.Paginacion;
using RecetasOCR.Application.Features.GruposReceta;

namespace RecetasOCR.API.Controllers;

[ApiController]
[Route("api/grupos-receta")]
[Authorize]
public class GruposRecetaController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize("GRUPOS.VER")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<GrupoRecetaDto>>>> GetAll(
        [FromQuery] FiltrosGrupoDto filtros, CancellationToken ct)
    {
        var result = await mediator.Send(new GetGruposRecetaQuery(filtros), ct);
        return Ok(ApiResponse<PagedResultDto<GrupoRecetaDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize("GRUPOS.VER")]
    public async Task<ActionResult<ApiResponse<GrupoRecetaDetalleDto>>> GetById(
        Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetGrupoRecetaDetalleQuery(id), ct);
        return Ok(ApiResponse<GrupoRecetaDetalleDto>.Ok(result));
    }

    [HttpPost]
    [Authorize("IMAGENES.SUBIR.escribir")]
    public async Task<ActionResult<ApiResponse<GrupoRecetaDto>>> Crear(
        [FromBody] CrearGrupoRecetaCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(ApiResponse<GrupoRecetaDto>.Ok(result.Grupo));
    }
}
