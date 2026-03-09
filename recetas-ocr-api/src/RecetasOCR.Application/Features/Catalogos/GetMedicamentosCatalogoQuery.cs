using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Catalogos;
using RecetasOCR.Application.DTOs.Paginacion;

namespace RecetasOCR.Application.Features.Catalogos;

public record GetMedicamentosCatalogoQuery(
    string? Busqueda = null,
    int     Page     = 1,
    int     PageSize = 20
) : IRequest<PagedResultDto<MedicamentoCatalogoDto>>;

public class GetMedicamentosCatalogoQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetMedicamentosCatalogoQuery, PagedResultDto<MedicamentoCatalogoDto>>
{
    public async Task<PagedResultDto<MedicamentoCatalogoDto>> Handle(
        GetMedicamentosCatalogoQuery query,
        CancellationToken            ct)
    {
        var page        = Math.Max(1, query.Page);
        var pageSize    = Math.Clamp(query.PageSize, 1, 100);
        var offset      = (page - 1) * pageSize;
        var busquedaLike = query.Busqueda != null ? $"%{query.Busqueda}%" : null;

        var total = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value
                FROM   cat.Medicamentos
                WHERE  Activo = 1
                  AND  ({busquedaLike} IS NULL
                        OR NombreComercial LIKE {busquedaLike}
                        OR SustanciaActiva LIKE {busquedaLike})
                """)
            .FirstAsync(ct);

        if (total == 0)
            return PagedResultDto<MedicamentoCatalogoDto>.Empty(page, pageSize);

        var rows = await db.Database
            .SqlQuery<MedicamentoRow>($"""
                SELECT Id, NombreComercial, SustanciaActiva, Presentacion, Concentracion
                FROM   cat.Medicamentos
                WHERE  Activo = 1
                  AND  ({busquedaLike} IS NULL
                        OR NombreComercial LIKE {busquedaLike}
                        OR SustanciaActiva LIKE {busquedaLike})
                ORDER  BY NombreComercial ASC
                OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
                """)
            .ToListAsync(ct);

        var items = rows
            .Select(r => new MedicamentoCatalogoDto(
                r.Id, r.NombreComercial, r.SustanciaActiva, r.Presentacion, r.Concentracion))
            .ToList();

        return new PagedResultDto<MedicamentoCatalogoDto>(items, total, page, pageSize);
    }

    private sealed record MedicamentoRow(
        int     Id,
        string  NombreComercial,
        string? SustanciaActiva,
        string? Presentacion,
        string? Concentracion);
}
