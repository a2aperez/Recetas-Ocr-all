using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Catalogos;

namespace RecetasOCR.Application.Features.Catalogos;

public record GetViaAdministracionQuery : IRequest<List<ViaAdministracionDto>>;

public class GetViaAdministracionQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetViaAdministracionQuery, List<ViaAdministracionDto>>
{
    public async Task<List<ViaAdministracionDto>> Handle(
        GetViaAdministracionQuery _,
        CancellationToken         ct)
    {
        var rows = await db.Database
            .SqlQuery<ViaRow>($"""
                SELECT Id, Clave, Descripcion
                FROM   cat.ViasAdministracion
                ORDER  BY Descripcion ASC
                """)
            .ToListAsync(ct);

        return rows
            .Select(r => new ViaAdministracionDto(r.Id, r.Clave, r.Descripcion))
            .ToList();
    }

    private sealed record ViaRow(int Id, string Clave, string Descripcion);
}
