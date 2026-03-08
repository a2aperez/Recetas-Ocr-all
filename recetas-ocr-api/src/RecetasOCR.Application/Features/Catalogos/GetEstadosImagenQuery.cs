using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Catalogos;

namespace RecetasOCR.Application.Features.Catalogos;

public record GetEstadosImagenQuery : IRequest<List<EstadoDto>>;

public class GetEstadosImagenQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetEstadosImagenQuery, List<EstadoDto>>
{
    public async Task<List<EstadoDto>> Handle(
        GetEstadosImagenQuery _,
        CancellationToken     ct)
    {
        var rows = await db.Database
            .SqlQuery<EstadoRow>($"""
                SELECT Id, Clave, Descripcion
                FROM   cat.EstadosImagen
                ORDER  BY Orden ASC
                """)
            .ToListAsync(ct);

        return rows
            .Select(r => new EstadoDto(r.Id, r.Clave, r.Descripcion, null))
            .ToList();
    }

    private sealed record EstadoRow(int Id, string Clave, string Descripcion);
}
