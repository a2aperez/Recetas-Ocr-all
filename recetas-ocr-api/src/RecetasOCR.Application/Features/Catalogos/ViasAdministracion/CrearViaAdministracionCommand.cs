using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;

namespace RecetasOCR.Application.Features.Catalogos.ViasAdministracion;

// ── Command ───────────────────────────────────────────────────────────────────

public record CrearViaAdministracionCommand(
    string Clave,
    string Nombre
) : IRequest<int>, IAuditableCommand;

// ── Validator ─────────────────────────────────────────────────────────────────

public class CrearViaAdministracionCommandValidator
    : AbstractValidator<CrearViaAdministracionCommand>
{
    public CrearViaAdministracionCommandValidator()
    {
        RuleFor(x => x.Clave)
            .NotEmpty().WithMessage("La clave es obligatoria.")
            .MaximumLength(50).WithMessage("La clave no puede exceder 50 caracteres.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class CrearViaAdministracionCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser)
    : IRequestHandler<CrearViaAdministracionCommand, int>
{
    public async Task<int> Handle(
        CrearViaAdministracionCommand command,
        CancellationToken             ct)
    {
        var ahora    = DateTime.UtcNow;
        var username = currentUser.Username;

        // Verificar clave única
        var claveCount = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value
                FROM   cat.ViasAdministracion
                WHERE  Clave = {command.Clave}
                """)
            .FirstAsync(ct);

        if (claveCount > 0)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.Clave),
                    $"Ya existe una vía de administración con la clave '{command.Clave}'.")
            });

        // Nombre → columna Descripcion en DB
        var newId = await db.Database
            .SqlQuery<int>($"""
                INSERT INTO cat.ViasAdministracion
                    (Clave, Descripcion, ModificadoPor, FechaModificacion)
                OUTPUT INSERTED.Id
                VALUES ({command.Clave}, {command.Nombre}, {username}, {ahora})
                """)
            .FirstAsync(ct);

        return newId;
    }
}
