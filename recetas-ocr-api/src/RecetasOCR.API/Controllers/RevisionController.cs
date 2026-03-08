using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Paginacion;
using RecetasOCR.Application.Features.Revision;

namespace RecetasOCR.API.Controllers;

[ApiController]
[Route("api/revision")]
[Authorize]
public class RevisionController(IMediator mediator) : ControllerBase
{
    [HttpGet("cola")]
    [Authorize("REVISION.VER")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<ColaRevisionItemDto>>>> GetCola(
        [FromQuery] GetColaRevisionQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return Ok(ApiResponse<PagedResultDto<ColaRevisionItemDto>>.Ok(result));
    }

    [HttpPost("aprobar")]
    [Authorize("REVISION.APROBAR.escribir")]
    public async Task<ActionResult<ApiResponse<bool>>> Aprobar(
        [FromBody] AprobarImagenCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("rechazar")]
    [Authorize("REVISION.APROBAR.escribir")]
    public async Task<ActionResult<ApiResponse<bool>>> Rechazar(
        [FromBody] RechazarImagenCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("corregir-campo")]
    [Authorize("REVISION.APROBAR.escribir")]
    public async Task<ActionResult<ApiResponse<bool>>> CorregirCampo(
        [FromBody] CorregirCampoCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return Ok(ApiResponse<bool>.Ok(true));
    }
}
