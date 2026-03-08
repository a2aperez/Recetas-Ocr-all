using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Catalogos;

namespace RecetasOCR.Application.Features.Catalogos;

public record GetAseguradorasQuery : IRequest<List<AseguradoraDto>>;

public class GetAseguradorasQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetAseguradorasQuery, List<AseguradoraDto>>
{
    public async Task<List<AseguradoraDto>> Handle(
        GetAseguradorasQuery _,
        CancellationToken    ct)
    {
        var rows = await db.Database
            .SqlQuery<AseguradoraRow>($"""
                SELECT Id, Clave, Nombre, NombreCorto, RFC, Activo
                FROM   cat.Aseguradoras
                WHERE  Activo = 1
                ORDER  BY Nombre ASC
                """)
            .ToListAsync(ct);

        return rows
            .Select(r => new AseguradoraDto(r.Id, r.Nombre, r.Clave, r.NombreCorto, r.RFC, r.Activo))
            .ToList();
    }

    private sealed record AseguradoraRow(
        int     Id,
        string  Clave,
        string  Nombre,
        string? NombreCorto,
        string? RFC,
        bool    Activo);
}
