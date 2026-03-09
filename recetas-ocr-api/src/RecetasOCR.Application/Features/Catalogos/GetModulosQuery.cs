using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Catalogos;

namespace RecetasOCR.Application.Features.Catalogos;

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Retorna todos los módulos del sistema registrados en seg.Modulos.
/// Usado por administradores para ver qué permisos pueden asignarse.
/// </summary>
public record GetModulosQuery : IRequest<List<ModuloDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetModulosQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetModulosQuery, List<ModuloDto>>
{
    public async Task<List<ModuloDto>> Handle(
        GetModulosQuery   _,
        CancellationToken ct)
    {
        var rows = await db.Database
            .SqlQuery<ModuloRow>($"""
                SELECT Id, Clave, Nombre, Descripcion
                FROM   seg.Modulos
                WHERE  Activo = 1
                ORDER  BY Clave ASC
                """)
            .ToListAsync(ct);

        return rows
            .Select(r => new ModuloDto(r.Id, r.Clave, r.Nombre, r.Descripcion))
            .ToList();
    }

    private sealed record ModuloRow(
        int     Id,
        string  Clave,
        string  Nombre,
        string? Descripcion);
}
