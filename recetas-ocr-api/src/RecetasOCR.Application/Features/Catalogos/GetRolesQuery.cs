using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Catalogos;

namespace RecetasOCR.Application.Features.Catalogos;

public record GetRolesQuery : IRequest<List<RolDto>>;

public class GetRolesQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetRolesQuery, List<RolDto>>
{
    public async Task<List<RolDto>> Handle(
        GetRolesQuery     _,
        CancellationToken ct)
    {
        var rows = await db.Database
            .SqlQuery<RolRow>($"""
                SELECT Id, Clave, Nombre, Descripcion, Activo
                FROM   seg.Roles
                ORDER  BY Nombre ASC
                """)
            .ToListAsync(ct);

        return rows
            .Select(r => new RolDto(r.Id, r.Clave, r.Nombre, r.Descripcion, r.Activo))
            .ToList();
    }

    private sealed record RolRow(
        int     Id,
        string  Clave,
        string  Nombre,
        string? Descripcion,
        bool    Activo);
}
