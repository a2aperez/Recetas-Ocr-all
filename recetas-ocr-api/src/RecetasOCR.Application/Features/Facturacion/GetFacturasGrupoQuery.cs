using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Facturacion;

namespace RecetasOCR.Application.Features.Facturacion;

// ── Query ─────────────────────────────────────────────────────────────────────

public record GetFacturasGrupoQuery(Guid IdGrupo) : IRequest<List<CfdiDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetFacturasGrupoQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetFacturasGrupoQuery, List<CfdiDto>>
{
    public async Task<List<CfdiDto>> Handle(GetFacturasGrupoQuery query, CancellationToken ct)
    {
        var rows = await db.Database
            .SqlQuery<CfdiRow>($"""
                SELECT Id, IdPreFactura, UUID, Version, Total, Estado,
                       UrlBlobXML, UrlBlobPDF, NoCertificadoSAT, SelloSAT,
                       FechaTimbrado, FechaCreacion
                FROM   fac.CFDI
                WHERE  IdGrupo = {query.IdGrupo}
                ORDER  BY FechaCreacion DESC
                """)
            .ToListAsync(ct);

        return rows.Select(r => new CfdiDto(
            Id:              r.Id,
            IdPreFactura:    r.IdPreFactura,
            UUID:            r.UUID,
            Version:         r.Version,
            Total:           r.Total,
            Estado:          r.Estado,
            UrlXml:          r.UrlBlobXML,
            UrlPdf:          r.UrlBlobPDF,
            NoCertificadoSAT: r.NoCertificadoSAT,
            SelloSAT:        r.SelloSAT,
            FechaTimbrado:   r.FechaTimbrado,
            FechaCreacion:   r.FechaCreacion
        )).ToList();
    }

    private record CfdiRow(
        Guid     Id,
        Guid     IdPreFactura,
        string   UUID,
        string   Version,
        decimal  Total,
        string   Estado,
        string   UrlBlobXML,
        string?  UrlBlobPDF,
        string?  NoCertificadoSAT,
        string?  SelloSAT,
        DateTime FechaTimbrado,
        DateTime FechaCreacion);
}
