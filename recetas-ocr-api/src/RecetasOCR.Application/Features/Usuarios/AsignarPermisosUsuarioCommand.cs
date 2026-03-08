using MediatR;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Usuarios;
using RecetasOCR.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Application.Features.Usuarios;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Reemplaza en bloque los permisos individuales de un usuario.
/// Los PermisosUsuario sobreescriben (no acumulan) los del rol.
/// Cada Modulo corresponde a la Clave de seg.Modulos.
/// </summary>
public record AsignarPermisosUsuarioCommand(
    Guid                    IdUsuario,
    List<PermisoUsuarioDto> Permisos
) : IRequest<bool>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class AsignarPermisosUsuarioCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser)
    : IRequestHandler<AsignarPermisosUsuarioCommand, bool>
{
    public async Task<bool> Handle(
        AsignarPermisosUsuarioCommand command,
        CancellationToken              ct)
    {
        // Verificar que el usuario existe
        var existe = await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM seg.Usuarios WHERE Id = {command.IdUsuario}")
            .FirstAsync(ct);

        if (existe == 0)
            throw new EntidadNoEncontradaException("Usuario", command.IdUsuario);

        var modificadoPor = currentUser.Username ?? "sistema";

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.Database.ExecuteSqlAsync($"""
                DELETE FROM seg.PermisosUsuario
                WHERE       IdUsuario = {command.IdUsuario}
                """, ct);

            foreach (var p in command.Permisos)
            {
                await db.Database.ExecuteSqlAsync($"""
                    INSERT INTO seg.PermisosUsuario
                        (IdUsuario, IdModulo, PuedeLeer, PuedeEscribir, PuedeEliminar,
                         Denegado, FechaAlta, ModificadoPor, FechaModificacion)
                    SELECT {command.IdUsuario}, m.Id,
                           {p.PuedeLeer}, {p.PuedeEscribir}, {p.PuedeEliminar},
                           {p.Denegado}, GETUTCDATE(), {modificadoPor}, GETUTCDATE()
                    FROM   seg.Modulos m
                    WHERE  m.Clave  = {p.Modulo}
                      AND  m.Activo = 1
                    """, ct);
            }

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        return true;
    }
}
