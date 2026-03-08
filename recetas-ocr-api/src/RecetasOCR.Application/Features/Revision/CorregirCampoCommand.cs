using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Enums;

namespace RecetasOCR.Application.Features.Revision;

// ──────────────────────────────────────────────────────────────────────────────
// Command
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Registra la corrección de un campo en el historial de auditoría.
/// SOLO hace INSERT en aud.HistorialCorrecciones — no modifica ninguna otra tabla.
/// La aplicación del valor nuevo (si aplica) la hace el handler específico
/// de cada entidad; este command es únicamente para trazabilidad.
/// TipoCorreccion debe ser un valor del enum TipoCorreccion del dominio.
/// </summary>
public record CorregirCampoCommand(
    Guid    IdImagen,
    string  Tabla,
    string  Campo,
    string? ValorAnterior,
    string  ValorNuevo,
    string  TipoCorreccion,
    Guid?   IdGrupo      = null,
    Guid?   IdMedicamento = null
) : IRequest<Unit>, IAuditableCommand;

// ──────────────────────────────────────────────────────────────────────────────
// Validator
// ──────────────────────────────────────────────────────────────────────────────

public class CorregirCampoCommandValidator : AbstractValidator<CorregirCampoCommand>
{
    private static readonly string[] _tiposCorreccionValidos =
        Enum.GetNames<TipoCorreccion>();

    public CorregirCampoCommandValidator()
    {
        RuleFor(x => x.IdImagen)
            .NotEmpty()
            .WithMessage("El IdImagen es obligatorio.");

        RuleFor(x => x.Tabla)
            .NotEmpty()
            .WithMessage("La tabla es obligatoria.")
            .MaximumLength(100)
            .WithMessage("La tabla no puede exceder 100 caracteres.");

        RuleFor(x => x.Campo)
            .NotEmpty()
            .WithMessage("El campo es obligatorio.")
            .MaximumLength(100)
            .WithMessage("El campo no puede exceder 100 caracteres.");

        RuleFor(x => x.ValorNuevo)
            .NotEmpty()
            .WithMessage("El valor nuevo es obligatorio.")
            .MaximumLength(500)
            .WithMessage("El valor nuevo no puede exceder 500 caracteres.");

        RuleFor(x => x.ValorAnterior)
            .MaximumLength(500)
            .WithMessage("El valor anterior no puede exceder 500 caracteres.")
            .When(x => x.ValorAnterior is not null);

        RuleFor(x => x.TipoCorreccion)
            .NotEmpty()
            .WithMessage("El tipo de corrección es obligatorio.")
            .Must(t => _tiposCorreccionValidos.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"TipoCorreccion debe ser: {string.Join(", ", _tiposCorreccionValidos)}.");
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Handler
// ──────────────────────────────────────────────────────────────────────────────

public class CorregirCampoCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser,
    ILogger<CorregirCampoCommandHandler> logger)
    : IRequestHandler<CorregirCampoCommand, Unit>
{
    public async Task<Unit> Handle(
        CorregirCampoCommand command,
        CancellationToken    cancellationToken)
    {
        var ahora     = DateTime.UtcNow;
        var usuarioId = currentUser.UserId!.Value;

        // Solo INSERT en aud.HistorialCorrecciones — no modifica nada más.
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO aud.HistorialCorrecciones
                (IdImagen, IdGrupo, IdMedicamento,
                 Tabla, Campo, ValorAnterior, ValorNuevo,
                 TipoCorreccion, IdUsuario, FechaCorreccion)
            VALUES
                ({command.IdImagen}, {command.IdGrupo}, {command.IdMedicamento},
                 {command.Tabla}, {command.Campo},
                 {command.ValorAnterior}, {command.ValorNuevo},
                 {command.TipoCorreccion}, {usuarioId}, {ahora})
            """, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "[Revision] Corrección registrada | Imagen: {IdImagen} | {Tabla}.{Campo} '{Ant}' → '{Nvo}'",
            command.IdImagen, command.Tabla, command.Campo,
            command.ValorAnterior, command.ValorNuevo);

        return Unit.Value;
    }
}
