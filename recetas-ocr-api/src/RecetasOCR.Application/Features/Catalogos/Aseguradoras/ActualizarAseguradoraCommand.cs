using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Catalogos.Aseguradoras;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Actualiza datos de una aseguradora.
/// No permite cambiar Clave ni IdAseguradoraPadre.
/// </summary>
public record ActualizarAseguradoraCommand(
    int     Id,
    string  Nombre,
    string? RazonSocial,
    string? RFC,
    bool    Activo
) : IRequest<Unit>, IAuditableCommand;

// ── Handler ───────────────────────────────────────────────────────────────────

public class ActualizarAseguradoraCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser)
    : IRequestHandler<ActualizarAseguradoraCommand, Unit>
{
    public async Task<Unit> Handle(
        ActualizarAseguradoraCommand command,
        CancellationToken            ct)
    {
        var ahora    = DateTime.UtcNow;
        var username = currentUser.Username;

        var rows = await db.Database.ExecuteSqlAsync($"""
            UPDATE cat.Aseguradoras
            SET    Nombre            = {command.Nombre},
                   NombreCorto       = {command.RazonSocial},
                   RFC               = {command.RFC},
                   Activo            = {command.Activo},
                   ModificadoPor     = {username},
                   FechaModificacion = {ahora}
            WHERE  Id = {command.Id}
            """, ct);

        if (rows == 0)
            throw new EntidadNoEncontradaException("Aseguradora", command.Id);

        return Unit.Value;
    }
}
