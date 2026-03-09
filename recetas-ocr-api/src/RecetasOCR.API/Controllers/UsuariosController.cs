using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Paginacion;
using RecetasOCR.Application.DTOs.Usuarios;
using RecetasOCR.Application.Features.Usuarios;

namespace RecetasOCR.API.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [Authorize("USUARIOS.ADMINISTRAR")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<UsuarioListaDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? busqueda = null, [FromQuery] bool? activo = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetUsuariosQuery(page, pageSize, busqueda, activo), ct);
        return Ok(ApiResponse<PagedResultDto<UsuarioListaDto>>.Ok(result));
    }

    [HttpGet("perfil")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UsuarioDetalleDto>>> GetPerfil(CancellationToken ct)
    {
        var result = await mediator.Send(new GetUsuarioByIdQuery(currentUser.UserId!.Value), ct);
        return Ok(ApiResponse<UsuarioDetalleDto>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize("USUARIOS.ADMINISTRAR")]
    public async Task<ActionResult<ApiResponse<UsuarioDetalleDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetUsuarioByIdQuery(id), ct);
        return Ok(ApiResponse<UsuarioDetalleDto>.Ok(result));
    }

    [HttpPost]
    [Authorize("USUARIOS.ADMINISTRAR")]
    public async Task<ActionResult<ApiResponse<CrearUsuarioResponseDto>>> Crear(
        [FromBody] CrearUsuarioCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(ApiResponse<CrearUsuarioResponseDto>.Ok(result));
    }

    [HttpPut("{id:guid}/estado")]
    [Authorize("USUARIOS.ADMINISTRAR")]
    public async Task<ActionResult<ApiResponse<bool>>> CambiarEstado(
        Guid id, [FromBody] CambiarEstadoUsuarioCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command with { IdUsuario = id }, ct);
        return Ok(ApiResponse<bool>.Ok(result));
    }

    [HttpPut("{id:guid}/password")]
    public async Task<ActionResult<ApiResponse<bool>>> CambiarPassword(
        Guid id, [FromBody] CambiarPasswordCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command with { IdUsuario = id }, ct);
        return Ok(ApiResponse<bool>.Ok(result));
    }

    [HttpPut("{id:guid}/permisos")]
    [Authorize("USUARIOS.ADMINISTRAR")]
    public async Task<ActionResult<ApiResponse<bool>>> AsignarPermisos(
        Guid id, [FromBody] AsignarPermisosUsuarioCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command with { IdUsuario = id }, ct);
        return Ok(ApiResponse<bool>.Ok(result));
    }
}
