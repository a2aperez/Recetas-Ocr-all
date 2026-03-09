using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Catalogos.Medicamentos;

// ── Command ───────────────────────────────────────────────────────────────────

public record ActualizarMedicamentoCommand(
    int     Id,
    string  NombreComercial,
    string? SustanciaActiva,
    string? Presentacion,
    string? CodigoEAN,      // aceptado pero no persiste — columna no existe en cat.Medicamentos
    string? ClaveSAT,
    bool    Activo
) : IRequest<Unit>, IAuditableCommand;

// ── Handler ───────────────────────────────────────────────────────────────────

public class ActualizarMedicamentoCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser)
    : IRequestHandler<ActualizarMedicamentoCommand, Unit>
{
    public async Task<Unit> Handle(
        ActualizarMedicamentoCommand command,
        CancellationToken            ct)
    {
        var ahora    = DateTime.UtcNow;
        var username = currentUser.Username;

        var rows = await db.Database.ExecuteSqlAsync($"""
            UPDATE cat.Medicamentos
            SET    NombreComercial   = {command.NombreComercial},
                   SustanciaActiva   = {command.SustanciaActiva},
                   Presentacion      = {command.Presentacion},
                   ClaveSAT          = {command.ClaveSAT},
                   Activo            = {command.Activo},
                   ModificadoPor     = {username},
                   FechaModificacion = {ahora}
            WHERE  Id = {command.Id}
            """, ct);

        if (rows == 0)
            throw new EntidadNoEncontradaException("Medicamento", command.Id);

        return Unit.Value;
    }
}
