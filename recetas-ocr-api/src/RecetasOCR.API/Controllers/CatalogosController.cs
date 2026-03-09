using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Catalogos;
using RecetasOCR.Application.DTOs.Paginacion;
using RecetasOCR.Application.Features.Catalogos;
using RecetasOCR.Application.Features.Catalogos.Aseguradoras;
using RecetasOCR.Application.Features.Catalogos.ConfiguracionOcr;
using RecetasOCR.Application.Features.Catalogos.Medicamentos;
using RecetasOCR.Application.Features.Catalogos.Parametros;
using RecetasOCR.Application.Features.Catalogos.ViasAdministracion;

namespace RecetasOCR.API.Controllers;

[ApiController]
[Route("api/catalogos")]
[Authorize]
public class CatalogosController(IMediator mediator) : ControllerBase
{
    // ── Aseguradoras ──────────────────────────────────────────────────────────

    [HttpGet("aseguradoras")]
    public async Task<ActionResult<ApiResponse<List<AseguradoraAdminDto>>>> GetAseguradoras(
        [FromQuery] bool incluyeInactivas = false,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAseguradorasQuery(incluyeInactivas), ct);
        return Ok(ApiResponse<List<AseguradoraAdminDto>>.Ok(result));
    }

    [HttpPost("aseguradoras")]
    [Authorize("CONFIG.EDITAR.escribir")]
    public async Task<ActionResult<ApiResponse<int>>> CrearAseguradora(
        [FromBody] CrearAseguradoraCommand command, CancellationToken ct)
    {
        var newId = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAseguradoras), ApiResponse<int>.Ok(newId));
    }

    [HttpPut("aseguradoras/{id:int}")]
    [Authorize("CONFIG.EDITAR.escribir")]
    public async Task<ActionResult<ApiResponse<bool>>> ActualizarAseguradora(
        int id, [FromBody] ActualizarAseguradoraRequest body, CancellationToken ct)
    {
        var command = new ActualizarAseguradoraCommand(
            id, body.Nombre, body.RazonSocial, body.RFC, body.Activo);
        await mediator.Send(command, ct);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    // ── Medicamentos ──────────────────────────────────────────────────────────

    [HttpGet("medicamentos")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<MedicamentoCatalogoDto>>>> GetMedicamentos(
        [FromQuery] string? busqueda,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool incluyeInactivos = false,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetMedicamentosCatalogoQuery(busqueda, page, pageSize, incluyeInactivos), ct);
        return Ok(ApiResponse<PagedResultDto<MedicamentoCatalogoDto>>.Ok(result));
    }

    [HttpPost("medicamentos")]
    [Authorize("CONFIG.EDITAR.escribir")]
    public async Task<ActionResult<ApiResponse<int>>> CrearMedicamento(
        [FromBody] CrearMedicamentoCommand command, CancellationToken ct)
    {
        var newId = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetMedicamentos), ApiResponse<int>.Ok(newId));
    }

    [HttpPut("medicamentos/{id:int}")]
    [Authorize("CONFIG.EDITAR.escribir")]
    public async Task<ActionResult<ApiResponse<bool>>> ActualizarMedicamento(
        int id, [FromBody] ActualizarMedicamentoRequest body, CancellationToken ct)
    {
        var command = new ActualizarMedicamentoCommand(
            id, body.NombreComercial, body.SustanciaActiva,
            body.Presentacion, body.CodigoEAN, body.ClaveSAT, body.Activo);
        await mediator.Send(command, ct);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    // ── Vías de administración ────────────────────────────────────────────────

    [HttpGet("vias-administracion")]
    public async Task<ActionResult<ApiResponse<List<ViaAdministracionDto>>>> GetViasAdministracion(
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetViaAdministracionQuery(), ct);
        return Ok(ApiResponse<List<ViaAdministracionDto>>.Ok(result));
    }

    [HttpPost("vias-administracion")]
    [Authorize("CONFIG.EDITAR.escribir")]
    public async Task<ActionResult<ApiResponse<int>>> CrearViaAdministracion(
        [FromBody] CrearViaAdministracionCommand command, CancellationToken ct)
    {
        var newId = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetViasAdministracion), ApiResponse<int>.Ok(newId));
    }

    [HttpPut("vias-administracion/{id:int}")]
    [Authorize("CONFIG.EDITAR.escribir")]
    public async Task<ActionResult<ApiResponse<bool>>> ActualizarViaAdministracion(
        int id, [FromBody] ActualizarViaAdministracionRequest body, CancellationToken ct)
    {
        var command = new ActualizarViaAdministracionCommand(id, body.Nombre, body.Activo);
        await mediator.Send(command, ct);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    // ── Parámetros ────────────────────────────────────────────────────────────

    [HttpGet("parametros")]
    [Authorize("CONFIG.EDITAR")]
    public async Task<ActionResult<ApiResponse<List<ParametroDto>>>> GetParametros(CancellationToken ct)
    {
        var result = await mediator.Send(new GetParametrosQuery(), ct);
        return Ok(ApiResponse<List<ParametroDto>>.Ok(result));
    }

    [HttpPut("parametros/{clave}")]
    [Authorize("CONFIG.EDITAR.escribir")]
    public async Task<ActionResult<ApiResponse<bool>>> ActualizarParametro(
        string clave, [FromBody] ActualizarParametroRequest body, CancellationToken ct)
    {
        var command = new ActualizarParametroCommand(clave, body.Valor);
        await mediator.Send(command, ct);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    // ── Configuraciones OCR ───────────────────────────────────────────────────

    [HttpGet("configuraciones-ocr")]
    [Authorize("CONFIG.EDITAR")]
    public async Task<ActionResult<ApiResponse<List<ConfiguracionOcrDto>>>> GetConfiguracionesOcr(
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetConfiguracionesOcrQuery(), ct);
        return Ok(ApiResponse<List<ConfiguracionOcrDto>>.Ok(result));
    }

    [HttpPut("configuraciones-ocr/{id:int}")]
    [Authorize("CONFIG.EDITAR.escribir")]
    public async Task<ActionResult<ApiResponse<bool>>> ActualizarConfiguracionOcr(
        int id, [FromBody] ActualizarConfiguracionOcrRequest body, CancellationToken ct)
    {
        var command = new ActualizarConfiguracionOcrCommand(
            id, body.Nombre, body.UrlBase, body.ApiKey,
            body.EsPrincipal, body.Activo, body.ConfigJson);
        await mediator.Send(command, ct);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    // ── Estados ───────────────────────────────────────────────────────────────

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

    // ── Roles y módulos ───────────────────────────────────────────────────────

    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<List<RolDto>>>> GetRoles(CancellationToken ct)
    {
        var result = await mediator.Send(new GetRolesQuery(), ct);
        return Ok(ApiResponse<List<RolDto>>.Ok(result));
    }

    [HttpGet("modulos")]
    [Authorize("USUARIOS.ADMINISTRAR")]
    public async Task<ActionResult<ApiResponse<List<ModuloDto>>>> GetModulos(CancellationToken ct)
    {
        var result = await mediator.Send(new GetModulosQuery(), ct);
        return Ok(ApiResponse<List<ModuloDto>>.Ok(result));
    }
}

// ── Request body records ──────────────────────────────────────────────────────

public record ActualizarAseguradoraRequest(
    string  Nombre,
    string? RazonSocial,
    string? RFC,
    bool    Activo);

public record ActualizarMedicamentoRequest(
    string  NombreComercial,
    string? SustanciaActiva,
    string? Presentacion,
    string? CodigoEAN,
    string? ClaveSAT,
    bool    Activo);

public record ActualizarViaAdministracionRequest(string Nombre, bool Activo);

public record ActualizarParametroRequest(string Valor);

public record ActualizarConfiguracionOcrRequest(
    string  Nombre,
    string  UrlBase,
    string? ApiKey,
    bool    EsPrincipal,
    bool    Activo,
    string? ConfigJson);

