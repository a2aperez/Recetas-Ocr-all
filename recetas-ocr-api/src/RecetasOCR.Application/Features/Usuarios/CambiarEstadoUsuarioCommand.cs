using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Usuarios;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Activa o desactiva un usuario.
/// Al desactivar también revoca todas sus sesiones activas.
/// No puede aplicarse sobre el propio usuario autenticado.
/// </summary>
public record CambiarEstadoUsuarioCommand(
    Guid IdUsuario,
    bool Activo
) : IRequest<bool>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class CambiarEstadoUsuarioCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser)
    : IRequestHandler<CambiarEstadoUsuarioCommand, bool>
{
    public async Task<bool> Handle(
        CambiarEstadoUsuarioCommand command,
        CancellationToken           ct)
    {
        // No puede desactivar su propia cuenta
        if (!command.Activo && currentUser.UserId == command.IdUsuario)
            throw new InvalidOperationException("No puedes desactivar tu propia cuenta.");

        // Verificar que el usuario existe
        var existe = await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM seg.Usuarios WHERE Id = {command.IdUsuario}")
            .FirstAsync(ct);

        if (existe == 0)
            throw new EntidadNoEncontradaException("Usuario", command.IdUsuario);

        var modificadoPor = currentUser.Username ?? "sistema";
        var activo        = command.Activo ? 1 : 0;

        await db.Database.ExecuteSqlAsync($"""
            UPDATE seg.Usuarios
            SET    Activo             = {activo},
                   ModificadoPor      = {modificadoPor},
                   FechaActualizacion = GETUTCDATE(),
                   FechaModificacion  = GETUTCDATE()
            WHERE  Id = {command.IdUsuario}
            """, ct);

        // Al desactivar: revocar todas las sesiones activas
        if (!command.Activo)
        {
            await db.Database.ExecuteSqlAsync($"""
                UPDATE seg.Sesiones
                SET    Estado            = 'REVOCADA',
                       MotivoRevocacion  = 'Cuenta desactivada por administrador'
                WHERE  IdUsuario = {command.IdUsuario}
                  AND  Estado    = 'ACTIVA'
                """, ct);
        }

        return true;
    }
}
