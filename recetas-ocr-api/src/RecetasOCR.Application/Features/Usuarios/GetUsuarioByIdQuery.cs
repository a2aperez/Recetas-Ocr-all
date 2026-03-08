using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Auth;
using RecetasOCR.Application.DTOs.Usuarios;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Usuarios;

public record GetUsuarioByIdQuery(Guid Id) : IRequest<UsuarioDetalleDto>;

public class GetUsuarioByIdQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetUsuarioByIdQuery, UsuarioDetalleDto>
{
    public async Task<UsuarioDetalleDto> Handle(
        GetUsuarioByIdQuery query,
        CancellationToken   ct)
    {
        var row = await db.Database
            .SqlQuery<UsuarioDetalleRow>($"""
                SELECT u.Id, u.Username, u.Email, u.NombreCompleto,
                       r.Nombre AS NombreRol, u.Activo,
                       u.UltimoAcceso, u.FechaAlta AS FechaCreacion,
                       u.RequiereCambioPassword, u.IdRol
                FROM   seg.Usuarios u
                INNER  JOIN seg.Roles r ON r.Id = u.IdRol
                WHERE  u.Id = {query.Id}
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntidadNoEncontradaException("Usuario", query.Id);

        var permisosRol = await db.Database
            .SqlQuery<PermisoRow>($"""
                SELECT m.Clave AS Modulo,
                       pr.PuedeLeer, pr.PuedeEscribir, pr.PuedeEliminar,
                       CAST(0 AS BIT) AS Denegado
                FROM   seg.PermisosRol pr
                INNER  JOIN seg.Modulos m ON m.Id = pr.IdModulo
                WHERE  pr.IdRol  = {row.IdRol}
                  AND  m.Activo  = 1
                """)
            .ToListAsync(ct);

        var permisosUsuario = await db.Database
            .SqlQuery<PermisoRow>($"""
                SELECT m.Clave AS Modulo,
                       pu.PuedeLeer, pu.PuedeEscribir, pu.PuedeEliminar,
                       pu.Denegado
                FROM   seg.PermisosUsuario pu
                INNER  JOIN seg.Modulos m ON m.Id = pu.IdModulo
                WHERE  pu.IdUsuario = {query.Id}
                  AND  m.Activo     = 1
                """)
            .ToListAsync(ct);

        var permisos = CombinarPermisos(permisosRol, permisosUsuario);

        return new UsuarioDetalleDto(
            row.Id, row.Username, row.Email, row.NombreCompleto,
            row.NombreRol, row.Activo, row.UltimoAcceso, row.FechaCreacion,
            row.RequiereCambioPassword, permisos);
    }

    // PermisosUsuario con Denegado=true sobreescribe al permiso de rol.
    // Sin Denegado, los PermisosUsuario reemplazan (no acumulan) los de rol.
    internal static List<PermisoDto> CombinarPermisos(
        List<PermisoRow> rol, List<PermisoRow> usuario)
    {
        var efectivos = rol.ToDictionary(
            p => p.Modulo,
            p => new PermisoDto(p.Modulo, p.PuedeLeer, p.PuedeEscribir, p.PuedeEliminar));

        foreach (var pu in usuario)
        {
            if (pu.Denegado)
                efectivos[pu.Modulo] = new PermisoDto(pu.Modulo, false, false, false);
            else
                efectivos[pu.Modulo] = new PermisoDto(pu.Modulo, pu.PuedeLeer, pu.PuedeEscribir, pu.PuedeEliminar);
        }

        return [.. efectivos.Values.OrderBy(p => p.Modulo)];
    }

    internal sealed record PermisoRow(
        string Modulo,
        bool   PuedeLeer,
        bool   PuedeEscribir,
        bool   PuedeEliminar,
        bool   Denegado);

    private sealed record UsuarioDetalleRow(
        Guid      Id,
        string    Username,
        string    Email,
        string    NombreCompleto,
        string    NombreRol,
        bool      Activo,
        DateTime? UltimoAcceso,
        DateTime  FechaCreacion,
        bool      RequiereCambioPassword,
        int       IdRol);
}
