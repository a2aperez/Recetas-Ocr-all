using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;

namespace RecetasOCR.Application.Features.Auth;

public record LogoutCommand : IRequest<bool>;

public class LogoutCommandHandler(
    IRecetasOcrDbContext           db,
    ICurrentUserService            currentUser,
    ILogger<LogoutCommandHandler>  logger)
    : IRequestHandler<LogoutCommand, bool>
{
    public async Task<bool> Handle(LogoutCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        await db.Database.ExecuteSqlAsync($"""
            UPDATE seg.Sesiones
            SET    Estado           = 'CERRADA',
                   MotivoRevocacion = 'LOGOUT_USUARIO'
            WHERE  IdUsuario = {userId}
              AND  Estado    = 'ACTIVA'
            """, ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Auth] LOGOUT — {Username}", currentUser.Username);
        return true;
    }
}
