using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;

namespace RecetasOCR.Application.Features.Catalogos.Medicamentos;

// ── Command ───────────────────────────────────────────────────────────────────

public record CrearMedicamentoCommand(
    string  NombreComercial,
    string? SustanciaActiva,
    string? Presentacion,
    string? CodigoEAN,      // aceptado pero no persiste — columna no existe en cat.Medicamentos
    string? ClaveSAT
) : IRequest<int>, IAuditableCommand;

// ── Validator ─────────────────────────────────────────────────────────────────

public class CrearMedicamentoCommandValidator : AbstractValidator<CrearMedicamentoCommand>
{
    public CrearMedicamentoCommandValidator()
    {
        RuleFor(x => x.NombreComercial)
            .NotEmpty().WithMessage("El nombre comercial es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre comercial no puede exceder 200 caracteres.");

        RuleFor(x => x.SustanciaActiva)
            .MaximumLength(200).WithMessage("La sustancia activa no puede exceder 200 caracteres.")
            .When(x => x.SustanciaActiva != null);

        RuleFor(x => x.Presentacion)
            .MaximumLength(100).WithMessage("La presentación no puede exceder 100 caracteres.")
            .When(x => x.Presentacion != null);

        RuleFor(x => x.ClaveSAT)
            .MaximumLength(20).WithMessage("La clave SAT no puede exceder 20 caracteres.")
            .When(x => x.ClaveSAT != null);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class CrearMedicamentoCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser)
    : IRequestHandler<CrearMedicamentoCommand, int>
{
    public async Task<int> Handle(
        CrearMedicamentoCommand command,
        CancellationToken       ct)
    {
        var ahora    = DateTime.UtcNow;
        var username = currentUser.Username;

        var newId = await db.Database
            .SqlQuery<int>($"""
                INSERT INTO cat.Medicamentos
                    (NombreComercial, SustanciaActiva, Presentacion,
                     ClaveSAT, Activo, FechaAlta, ModificadoPor, FechaModificacion)
                OUTPUT INSERTED.Id
                VALUES
                    ({command.NombreComercial}, {command.SustanciaActiva},
                     {command.Presentacion}, {command.ClaveSAT},
                     1, {ahora}, {username}, {ahora})
                """)
            .FirstAsync(ct);

        return newId;
    }
}
