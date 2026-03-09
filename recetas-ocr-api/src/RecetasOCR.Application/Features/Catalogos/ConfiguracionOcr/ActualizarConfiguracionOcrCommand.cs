using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Catalogos.ConfiguracionOcr;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Actualiza una configuración OCR.
/// Si ApiKey no es null/empty → actualiza ApiKeyEncriptada.
/// Si EsPrincipal = true → desmarca todas las demás como no-principal primero.
/// </summary>
public record ActualizarConfiguracionOcrCommand(
    int     Id,
    string  Nombre,
    string  UrlBase,
    string? ApiKey,
    bool    EsPrincipal,
    bool    Activo,
    string? ConfigJson
) : IRequest<Unit>, IAuditableCommand;

// ── Handler ───────────────────────────────────────────────────────────────────

public class ActualizarConfiguracionOcrCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser)
    : IRequestHandler<ActualizarConfiguracionOcrCommand, Unit>
{
    public async Task<Unit> Handle(
        ActualizarConfiguracionOcrCommand command,
        CancellationToken                 ct)
    {
        var ahora    = DateTime.UtcNow;
        var username = currentUser.Username;

        // Verificar que existe
        var existe = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value
                FROM   cfg.ConfiguracionesOCR
                WHERE  Id = {command.Id}
                """)
            .FirstAsync(ct);

        if (existe == 0)
            throw new EntidadNoEncontradaException("ConfiguracionOcr", command.Id);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // 1. Si EsPrincipal = true → desmarcar todas las demás
        if (command.EsPrincipal)
        {
            await db.Database.ExecuteSqlAsync($"""
                UPDATE cfg.ConfiguracionesOCR
                SET    EsPrincipal = 0,
                       ModificadoPor = {username},
                       FechaModificacion = {ahora}
                WHERE  Id <> {command.Id}
                """, ct);
        }

        // 2. UPDATE de la configuración principal
        //    Si ApiKey viene informada, actualizarla; si no, mantener la actual
        if (!string.IsNullOrEmpty(command.ApiKey))
        {
            await db.Database.ExecuteSqlAsync($"""
                UPDATE cfg.ConfiguracionesOCR
                SET    Nombre             = {command.Nombre},
                       UrlBase            = {command.UrlBase},
                       ApiKeyEncriptada   = {command.ApiKey},
                       EsPrincipal        = {command.EsPrincipal},
                       Activo             = {command.Activo},
                       ConfigJson         = {command.ConfigJson},
                       FechaActualizacion = {ahora},
                       ModificadoPor      = {username},
                       FechaModificacion  = {ahora}
                WHERE  Id = {command.Id}
                """, ct);
        }
        else
        {
            await db.Database.ExecuteSqlAsync($"""
                UPDATE cfg.ConfiguracionesOCR
                SET    Nombre             = {command.Nombre},
                       UrlBase            = {command.UrlBase},
                       EsPrincipal        = {command.EsPrincipal},
                       Activo             = {command.Activo},
                       ConfigJson         = {command.ConfigJson},
                       FechaActualizacion = {ahora},
                       ModificadoPor      = {username},
                       FechaModificacion  = {ahora}
                WHERE  Id = {command.Id}
                """, ct);
        }

        await tx.CommitAsync(ct);
        return Unit.Value;
    }
}
