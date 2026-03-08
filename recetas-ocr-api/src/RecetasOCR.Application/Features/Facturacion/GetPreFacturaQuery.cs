using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Facturacion;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Facturacion;

// ── Query ─────────────────────────────────────────────────────────────────────

public record GetPreFacturaQuery(Guid IdGrupo) : IRequest<PreFacturaDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetPreFacturaQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetPreFacturaQuery, PreFacturaDto>
{
    public async Task<PreFacturaDto> Handle(GetPreFacturaQuery query, CancellationToken ct)
    {
        var rows = await db.Database
            .SqlQuery<PreFacturaDetailRow>($"""
                SELECT TOP 1
                    pf.Id, pf.IdGrupo, pf.Estado,
                    pf.Subtotal, pf.TotalIVA AS IVA, pf.Total,
                    pf.FechaGeneracion AS FechaCreacion, pf.FechaModificacion,
                    r.RFC, r.NombreRazonSocial,
                    uc.Clave AS UsoCFDIClave,
                    mp.Clave AS MetodoPagoClave,
                    fp.Clave AS FormaPagoClave
                FROM   fac.PreFacturas pf
                INNER JOIN fac.Receptores  r  ON r.Id  = pf.IdReceptor
                INNER JOIN cat.UsoCFDI     uc ON uc.Id = pf.UsoCFDIId
                INNER JOIN cat.MetodosPago mp ON mp.Id = pf.MetodoPagoId
                INNER JOIN cat.FormasPago  fp ON fp.Id = pf.FormaPagoId
                WHERE  pf.IdGrupo = {query.IdGrupo}
                  AND  pf.Estado NOT IN ('CANCELADA', 'ERROR')
                ORDER  BY pf.FechaGeneracion DESC
                """)
            .ToListAsync(ct);

        var pf = rows.FirstOrDefault()
            ?? throw new EntidadNoEncontradaException("PreFactura", query.IdGrupo);

        var conceptos = await db.Database
            .SqlQuery<ConceptoRow>($"""
                SELECT pp.Id, pp.NumeroLinea, pp.Descripcion,
                       pp.ClaveProdServ, pp.Cantidad, pp.ValorUnitario,
                       pp.Importe, pp.IVAImporte AS IVA
                FROM   fac.PartidasPreFactura pp
                WHERE  pp.IdPreFactura = {pf.Id}
                ORDER  BY pp.NumeroLinea
                """)
            .ToListAsync(ct);

        return new PreFacturaDto(
            Id:                pf.Id,
            IdGrupo:           pf.IdGrupo,
            Estado:            pf.Estado,
            RFC:               pf.RFC,
            NombreFiscal:      pf.NombreRazonSocial,
            UsoCFDI:           pf.UsoCFDIClave,
            MetodoPago:        pf.MetodoPagoClave,
            FormaPago:         pf.FormaPagoClave,
            Subtotal:          pf.Subtotal,
            IVA:               pf.IVA,
            Total:             pf.Total,
            Conceptos:         conceptos.Select(c => new ConceptoFacturaDto(
                                   Id:                 c.Id,
                                   NumeroPrescripcion: c.NumeroLinea,
                                   Descripcion:        c.Descripcion,
                                   ClaveSAT:           c.ClaveProdServ,
                                   Cantidad:           c.Cantidad,
                                   PrecioUnitario:     c.ValorUnitario,
                                   Importe:            c.Importe,
                                   IVA:                c.IVA
                               )).ToList(),
            FechaCreacion:     pf.FechaCreacion,
            FechaModificacion: pf.FechaModificacion
        );
    }

    private record PreFacturaDetailRow(
        Guid     Id,
        Guid     IdGrupo,
        string   Estado,
        decimal  Subtotal,
        decimal  IVA,
        decimal  Total,
        DateTime FechaCreacion,
        DateTime FechaModificacion,
        string   RFC,
        string   NombreRazonSocial,
        string   UsoCFDIClave,
        string   MetodoPagoClave,
        string   FormaPagoClave);

    private record ConceptoRow(
        Guid    Id,
        int     NumeroLinea,
        string  Descripcion,
        string  ClaveProdServ,
        decimal Cantidad,
        decimal ValorUnitario,
        decimal Importe,
        decimal IVA);
}
