using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Paginacion;
using RecetasOCR.Application.DTOs.Usuarios;

namespace RecetasOCR.Application.Features.Usuarios;

public record GetUsuariosQuery(
    int     Page     = 1,
    int     PageSize = 20,
    string? Busqueda = null
) : IRequest<PagedResultDto<UsuarioListaDto>>;

public class GetUsuariosQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetUsuariosQuery, PagedResultDto<UsuarioListaDto>>
{
    public async Task<PagedResultDto<UsuarioListaDto>> Handle(
        GetUsuariosQuery  query,
        CancellationToken ct)
    {
        var page        = Math.Max(1, query.Page);
        var pageSize    = Math.Clamp(query.PageSize, 1, 100);
        var offset      = (page - 1) * pageSize;
        var busquedaLike = query.Busqueda != null ? $"%{query.Busqueda}%" : null;

        var total = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value
                FROM   seg.Usuarios u
                WHERE  u.Activo = 1
                  AND  ({busquedaLike} IS NULL
                        OR u.Username       LIKE {busquedaLike}
                        OR u.Email          LIKE {busquedaLike}
                        OR u.NombreCompleto LIKE {busquedaLike})
                """)
            .FirstAsync(ct);

        if (total == 0)
            return PagedResultDto<UsuarioListaDto>.Empty(page, pageSize);

        var rows = await db.Database
            .SqlQuery<UsuarioListaRow>($"""
                SELECT u.Id, u.Username, u.Email, u.NombreCompleto,
                       r.Nombre AS NombreRol, u.Activo,
                       u.UltimoAcceso, u.FechaAlta AS FechaCreacion
                FROM   seg.Usuarios u
                INNER  JOIN seg.Roles r ON r.Id = u.IdRol
                WHERE  u.Activo = 1
                  AND  ({busquedaLike} IS NULL
                        OR u.Username       LIKE {busquedaLike}
                        OR u.Email          LIKE {busquedaLike}
                        OR u.NombreCompleto LIKE {busquedaLike})
                ORDER  BY u.NombreCompleto ASC
                OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
                """)
            .ToListAsync(ct);

        var items = rows
            .Select(r => new UsuarioListaDto(
                r.Id, r.Username, r.Email, r.NombreCompleto,
                r.NombreRol, r.Activo, r.UltimoAcceso, r.FechaCreacion))
            .ToList();

        return new PagedResultDto<UsuarioListaDto>(items, total, page, pageSize);
    }

    private sealed record UsuarioListaRow(
        Guid      Id,
        string    Username,
        string    Email,
        string    NombreCompleto,
        string    NombreRol,
        bool      Activo,
        DateTime? UltimoAcceso,
        DateTime  FechaCreacion);
}
