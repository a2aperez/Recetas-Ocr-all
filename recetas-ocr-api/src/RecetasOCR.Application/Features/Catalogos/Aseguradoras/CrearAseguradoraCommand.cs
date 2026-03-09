using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Catalogos.Aseguradoras;

// ── Command ───────────────────────────────────────────────────────────────────

public record CrearAseguradoraCommand(
    string  Nombre,
    string  Clave,
    string? RazonSocial,
    string? RFC,
    int?    IdAseguradoraPadre
) : IRequest<int>, IAuditableCommand;

// ── Validator ─────────────────────────────────────────────────────────────────

public class CrearAseguradoraCommandValidator : AbstractValidator<CrearAseguradoraCommand>
{
    // RFC mexicano: 3-4 letras/caracteres + 6 dígitos fecha + 3 homoclave
    private static readonly Regex RfcRegex =
        new(@"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public CrearAseguradoraCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

        RuleFor(x => x.Clave)
            .NotEmpty().WithMessage("La clave es obligatoria.")
            .MaximumLength(50).WithMessage("La clave no puede exceder 50 caracteres.")
            .Matches(@"^[A-Z0-9_\-]+$").WithMessage("La clave solo puede contener letras mayúsculas, números, guiones y guiones bajos.");

        RuleFor(x => x.RazonSocial)
            .MaximumLength(50).WithMessage("La razón social no puede exceder 50 caracteres.")
            .When(x => x.RazonSocial != null);

        RuleFor(x => x.RFC)
            .MaximumLength(13).WithMessage("El RFC no puede exceder 13 caracteres.")
            .Matches(RfcRegex).WithMessage("El RFC no tiene formato válido (ej: XAXX010101000 o XAXX010101ABC).")
            .When(x => !string.IsNullOrEmpty(x.RFC));
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class CrearAseguradoraCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser)
    : IRequestHandler<CrearAseguradoraCommand, int>
{
    public async Task<int> Handle(
        CrearAseguradoraCommand command,
        CancellationToken       ct)
    {
        var ahora    = DateTime.UtcNow;
        var username = currentUser.Username;

        // 1. Clave única
        var claveCount = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value
                FROM   cat.Aseguradoras
                WHERE  Clave = {command.Clave}
                """)
            .FirstAsync(ct);

        if (claveCount > 0)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.Clave),
                    $"Ya existe una aseguradora con la clave '{command.Clave}'.")
            });

        // 2. Validar padre — máximo 2 niveles de jerarquía
        if (command.IdAseguradoraPadre.HasValue)
        {
            var padre = await db.Database
                .SqlQuery<PadreRow>($"""
                    SELECT Id, IdAseguradoraPadre
                    FROM   cat.Aseguradoras
                    WHERE  Id = {command.IdAseguradoraPadre.Value}
                    """)
                .FirstOrDefaultAsync(ct);

            if (padre is null)
                throw new EntidadNoEncontradaException(
                    "AseguradoraPadre", command.IdAseguradoraPadre.Value);

            if (padre.IdAseguradoraPadre.HasValue)
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(command.IdAseguradoraPadre),
                        "No se permiten más de 2 niveles de jerarquía. " +
                        "La aseguradora padre ya tiene un padre asignado.")
                });
        }

        // 3. INSERT — devuelve el nuevo Id
        var newId = await db.Database
            .SqlQuery<int>($"""
                INSERT INTO cat.Aseguradoras
                    (IdAseguradoraPadre, Clave, Nombre, NombreCorto, RFC,
                     Activo, FechaAlta, ModificadoPor, FechaModificacion)
                OUTPUT INSERTED.Id
                VALUES
                    ({command.IdAseguradoraPadre}, {command.Clave}, {command.Nombre},
                     {command.RazonSocial}, {command.RFC},
                     1, {ahora}, {username}, {ahora})
                """)
            .FirstAsync(ct);

        return newId;
    }

    private sealed record PadreRow(int Id, int? IdAseguradoraPadre);
}
