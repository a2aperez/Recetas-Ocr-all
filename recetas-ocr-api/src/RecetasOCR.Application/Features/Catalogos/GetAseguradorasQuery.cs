using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Catalogos;

namespace RecetasOCR.Application.Features.Catalogos;

/// <param name="IncluyeInactivas">
/// false (default): solo devuelve Activo=1 — para dropdowns en la app.
/// true: devuelve todas incluyendo inactivas — para pantallas de administración.
/// </param>
public record GetAseguradorasQuery(bool IncluyeInactivas = false)
    : IRequest<List<AseguradoraAdminDto>>;

public class GetAseguradorasQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetAseguradorasQuery, List<AseguradoraAdminDto>>
{
    public async Task<List<AseguradoraAdminDto>> Handle(
        GetAseguradorasQuery query,
        CancellationToken    ct)
    {
        var rows = await db.Database
            .SqlQuery<AseguradoraAdminRow>($"""
                SELECT a.Id, a.Clave, a.Nombre, a.NombreCorto, a.RFC, a.Activo,
                       a.IdAseguradoraPadre,
                       p.Nombre AS NombrePadre
                FROM   cat.Aseguradoras a
                LEFT   JOIN cat.Aseguradoras p ON p.Id = a.IdAseguradoraPadre
                WHERE  ({query.IncluyeInactivas} = 1 OR a.Activo = 1)
                ORDER  BY a.Nombre ASC
                """)
            .ToListAsync(ct);

        return rows
            .Select(r => new AseguradoraAdminDto(
                r.Id, r.Clave, r.Nombre, r.NombreCorto, r.RFC, r.Activo,
                r.IdAseguradoraPadre, r.NombrePadre))
            .ToList();
    }

    private sealed record AseguradoraAdminRow(
        int     Id,
        string  Clave,
        string  Nombre,
        string? NombreCorto,
        string? RFC,
        bool    Activo,
        int?    IdAseguradoraPadre,
        string? NombrePadre);
}
