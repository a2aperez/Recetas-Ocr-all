using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Catalogos;
using RecetasOCR.Application.DTOs.Paginacion;
using RecetasOCR.Application.Features.Catalogos;

namespace RecetasOCR.API.Controllers;

[ApiController]
[Route("api/catalogos")]
[Authorize]
public class CatalogosController(IMediator mediator) : ControllerBase
{
    [HttpGet("aseguradoras")]
    public async Task<ActionResult<ApiResponse<List<AseguradoraDto>>>> GetAseguradoras(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAseguradorasQuery(), ct);
        return Ok(ApiResponse<List<AseguradoraDto>>.Ok(result));
    }

    [HttpGet("medicamentos")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<MedicamentoCatalogoDto>>>> GetMedicamentos(
        [FromQuery] string? busqueda, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetMedicamentosCatalogoQuery(busqueda, page, pageSize), ct);
        return Ok(ApiResponse<PagedResultDto<MedicamentoCatalogoDto>>.Ok(result));
    }

    [HttpGet("vias-administracion")]
    public async Task<ActionResult<ApiResponse<List<ViaAdministracionDto>>>> GetViasAdministracion(CancellationToken ct)
    {
        var result = await mediator.Send(new GetViaAdministracionQuery(), ct);
        return Ok(ApiResponse<List<ViaAdministracionDto>>.Ok(result));
    }

    [HttpGet("estados-imagen")]
    public async Task<ActionResult<ApiResponse<List<EstadoDto>>>> GetEstadosImagen(CancellationToken ct)
    {
        var result = await mediator.Send(new GetEstadosImagenQuery(), ct);
        return Ok(ApiResponse<List<EstadoDto>>.Ok(result));
    }

    [HttpGet("estados-grupo")]
    public async Task<ActionResult<ApiResponse<List<EstadoDto>>>> GetEstadosGrupo(CancellationToken ct)
    {
        var result = await mediator.Send(new GetEstadosGrupoQuery(), ct);
        return Ok(ApiResponse<List<EstadoDto>>.Ok(result));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<List<RolDto>>>> GetRoles(CancellationToken ct)
    {
        var result = await mediator.Send(new GetRolesQuery(), ct);
        return Ok(ApiResponse<List<RolDto>>.Ok(result));
    }
}
