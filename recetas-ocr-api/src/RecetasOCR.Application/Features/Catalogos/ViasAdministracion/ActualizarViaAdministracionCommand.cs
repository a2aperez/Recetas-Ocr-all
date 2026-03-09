using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Catalogos.ViasAdministracion;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Actualiza el Nombre (Descripcion en DB) de una vía de administración.
/// Nota: cat.ViasAdministracion no tiene columna Activo; el parámetro se acepta
/// por contrato de API pero no se persiste en esta versión del esquema.
/// </summary>
public record ActualizarViaAdministracionCommand(
    int    Id,
    string Nombre,
    bool   Activo   // reservado — columna no existe en cat.ViasAdministracion
) : IRequest<Unit>, IAuditableCommand;

// ── Handler ───────────────────────────────────────────────────────────────────

public class ActualizarViaAdministracionCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser)
    : IRequestHandler<ActualizarViaAdministracionCommand, Unit>
{
    public async Task<Unit> Handle(
        ActualizarViaAdministracionCommand command,
        CancellationToken                  ct)
    {
        var ahora    = DateTime.UtcNow;
        var username = currentUser.Username;

        // Nombre → columna Descripcion en DB
        var rows = await db.Database.ExecuteSqlAsync($"""
            UPDATE cat.ViasAdministracion
            SET    Descripcion       = {command.Nombre},
                   ModificadoPor     = {username},
                   FechaModificacion = {ahora}
            WHERE  Id = {command.Id}
            """, ct);

        if (rows == 0)
            throw new EntidadNoEncontradaException("ViaAdministracion", command.Id);

        return Unit.Value;
    }
}
