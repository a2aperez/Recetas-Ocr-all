using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Facturacion;
using RecetasOCR.Application.DTOs.Paginacion;
using RecetasOCR.Application.Features.Facturacion;

namespace RecetasOCR.API.Controllers;

[ApiController]
[Route("api/facturacion")]
[Authorize]
public class FacturacionController(IMediator mediator) : ControllerBase
{
    [HttpGet("grupo/{idGrupo:guid}/prefactura")]
    [Authorize("FACTURACION.VER")]
    public async Task<ActionResult<ApiResponse<PreFacturaDto>>> GetPreFactura(Guid idGrupo, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPreFacturaQuery(idGrupo), ct);
        return Ok(ApiResponse<PreFacturaDto>.Ok(result));
    }

    [HttpPost("grupo/{idGrupo:guid}/generar-prefactura")]
    [Authorize("FACTURACION.GENERAR.escribir")]
    public async Task<ActionResult<ApiResponse<PreFacturaDto>>> GenerarPreFactura(Guid idGrupo, CancellationToken ct)
    {
        var result = await mediator.Send(new GenerarPreFacturaCommand(idGrupo), ct);
        return Ok(ApiResponse<PreFacturaDto>.Ok(result));
    }

    [HttpPut("grupo/{idGrupo:guid}/datos-fiscales")]
    [Authorize("FACTURACION.GENERAR.escribir")]
    public async Task<ActionResult<ApiResponse<bool>>> ActualizarDatosFiscales(
        Guid idGrupo, [FromBody] ActualizarDatosFiscalesRequest body, CancellationToken ct)
    {
        var result = await mediator.Send(
            new ActualizarDatosFiscalesCommand(idGrupo, body.RFC, body.NombreFiscal,
                body.UsoCFDI, body.MetodoPago, body.FormaPago, body.RegimenFiscal), ct);
        return Ok(ApiResponse<bool>.Ok(result));
    }

    [HttpPost("prefactura/{idPreFactura:guid}/timbrar")]
    [Authorize("FACTURACION.TIMBRAR.escribir")]
    public async Task<ActionResult<ApiResponse<CfdiDto>>> TimbrarCfdi(Guid idPreFactura, CancellationToken ct)
    {
        var result = await mediator.Send(new TimbrarCfdiCommand(idPreFactura), ct);
        return Ok(ApiResponse<CfdiDto>.Ok(result));
    }

    [HttpGet("grupo/{idGrupo:guid}/cfdis")]
    [Authorize("FACTURACION.VER")]
    public async Task<ActionResult<ApiResponse<List<CfdiDto>>>> GetFacturasGrupo(Guid idGrupo, CancellationToken ct)
    {
        var result = await mediator.Send(new GetFacturasGrupoQuery(idGrupo), ct);
        return Ok(ApiResponse<List<CfdiDto>>.Ok(result));
    }

    [HttpGet]
    [Authorize("FACTURACION.VER")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<FacturaResumenDto>>>> GetFacturas(
        [FromQuery] int?     idAseguradora,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] string?  rfc,
        [FromQuery] string?  estado,
        [FromQuery] int      page     = 1,
        [FromQuery] int      pageSize = 20,
        CancellationToken ct = default)
    {
        var filtros = new FiltrosFacturaDto(idAseguradora, fechaDesde, fechaHasta, rfc, estado, page, pageSize);
        var result  = await mediator.Send(new GetFacturasPaginadoQuery(filtros), ct);
        return Ok(ApiResponse<PagedResultDto<FacturaResumenDto>>.Ok(result));
    }
}

// ── Request body record ───────────────────────────────────────────────────────

public record ActualizarDatosFiscalesRequest(
    string  RFC,
    string  NombreFiscal,
    string  UsoCFDI,
    string  MetodoPago,
    string  FormaPago,
    string? RegimenFiscal
);
