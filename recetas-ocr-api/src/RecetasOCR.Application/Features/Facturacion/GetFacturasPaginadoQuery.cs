using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Facturacion;
using RecetasOCR.Application.DTOs.Paginacion;

namespace RecetasOCR.Application.Features.Facturacion;

// ── Query ─────────────────────────────────────────────────────────────────────

public record GetFacturasPaginadoQuery(FiltrosFacturaDto Filtros)
    : IRequest<PagedResultDto<FacturaResumenDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetFacturasPaginadoQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetFacturasPaginadoQuery, PagedResultDto<FacturaResumenDto>>
{
    public async Task<PagedResultDto<FacturaResumenDto>> Handle(
        GetFacturasPaginadoQuery query,
        CancellationToken        ct)
    {
        var f        = query.Filtros;
        var page     = Math.Max(1, f.Page);
        var pageSize = Math.Max(1, f.PageSize);
        var offset   = (page - 1) * pageSize;

        var total = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value
                FROM   fac.CFDI c
                INNER  JOIN rec.GruposReceta g ON g.Id = c.IdGrupo
                WHERE  ({f.IdAseguradora} IS NULL OR g.IdAseguradora = {f.IdAseguradora})
                  AND  ({f.FechaDesde}    IS NULL OR c.FechaTimbrado >= {f.FechaDesde})
                  AND  ({f.FechaHasta}    IS NULL OR c.FechaTimbrado <= {f.FechaHasta})
                  AND  ({f.RFC}           IS NULL OR c.RFCReceptor LIKE {(f.RFC != null ? "%" + f.RFC + "%" : null)})
                  AND  ({f.Estado}        IS NULL OR c.Estado = {f.Estado})
                """)
            .FirstAsync(ct);

        if (total == 0)
            return PagedResultDto<FacturaResumenDto>.Empty(page, pageSize);

        var rfcLike   = f.RFC != null ? "%" + f.RFC + "%" : null;

        var items = await db.Database
            .SqlQuery<FacturaResumenRow>($"""
                SELECT c.Id, c.UUID, g.NombrePaciente, c.RFCReceptor AS RFC,
                       c.Total, g.FechaConsulta, c.FechaTimbrado, c.Estado,
                       a.Nombre AS NombreAseguradora
                FROM   fac.CFDI c
                INNER  JOIN rec.GruposReceta  g ON g.Id  = c.IdGrupo
                INNER  JOIN cat.Aseguradoras  a ON a.Id  = g.IdAseguradora
                WHERE  ({f.IdAseguradora} IS NULL OR g.IdAseguradora = {f.IdAseguradora})
                  AND  ({f.FechaDesde}    IS NULL OR c.FechaTimbrado >= {f.FechaDesde})
                  AND  ({f.FechaHasta}    IS NULL OR c.FechaTimbrado <= {f.FechaHasta})
                  AND  ({rfcLike}         IS NULL OR c.RFCReceptor LIKE {rfcLike})
                  AND  ({f.Estado}        IS NULL OR c.Estado = {f.Estado})
                ORDER  BY c.FechaCreacion DESC
                OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
                """)
            .ToListAsync(ct);

        return new PagedResultDto<FacturaResumenDto>(
            items.Select(r => new FacturaResumenDto(
                r.Id, r.UUID, r.NombrePaciente, r.RFC, r.Total,
                r.FechaConsulta, r.FechaTimbrado, r.Estado, r.NombreAseguradora
            )).ToList(),
            total, page, pageSize);
    }

    private record FacturaResumenRow(
        Guid      Id,
        string    UUID,
        string?   NombrePaciente,
        string    RFC,
        decimal   Total,
        DateOnly? FechaConsulta,
        DateTime  FechaTimbrado,
        string    Estado,
        string    NombreAseguradora);
}
