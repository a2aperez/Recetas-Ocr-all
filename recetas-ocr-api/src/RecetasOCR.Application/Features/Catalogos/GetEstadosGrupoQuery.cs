using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Catalogos;

namespace RecetasOCR.Application.Features.Catalogos;

public record GetEstadosGrupoQuery : IRequest<List<EstadoDto>>;

public class GetEstadosGrupoQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetEstadosGrupoQuery, List<EstadoDto>>
{
    public async Task<List<EstadoDto>> Handle(
        GetEstadosGrupoQuery _,
        CancellationToken    ct)
    {
        var rows = await db.Database
            .SqlQuery<EstadoRow>($"""
                SELECT Id, Clave, Descripcion
                FROM   cat.EstadosGrupo
                ORDER  BY Orden ASC
                """)
            .ToListAsync(ct);

        return rows
            .Select(r => new EstadoDto(r.Id, r.Clave, r.Descripcion, null))
            .ToList();
    }

    private sealed record EstadoRow(int Id, string Clave, string Descripcion);
}
