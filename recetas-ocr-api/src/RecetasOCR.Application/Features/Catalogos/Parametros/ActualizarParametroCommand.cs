using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Catalogos.Parametros;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Actualiza el Valor de un parámetro identificado por su Clave.
/// No modifica Clave, TipoDato ni EsSecreto.
/// Invalida el caché de IParametrosService tras el UPDATE.
/// </summary>
public record ActualizarParametroCommand(
    string Clave,
    string Valor
) : IRequest<Unit>, IAuditableCommand;

// ── Validator ─────────────────────────────────────────────────────────────────

public class ActualizarParametroCommandValidator
    : AbstractValidator<ActualizarParametroCommand>
{
    public ActualizarParametroCommandValidator()
    {
        RuleFor(x => x.Clave)
            .NotEmpty().WithMessage("La clave del parámetro es obligatoria.")
            .MaximumLength(100).WithMessage("La clave no puede exceder 100 caracteres.");

        RuleFor(x => x.Valor)
            .NotNull().WithMessage("El valor no puede ser nulo.")
            .MaximumLength(1000).WithMessage("El valor no puede exceder 1000 caracteres.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class ActualizarParametroCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser,
    IParametrosService   parametros)
    : IRequestHandler<ActualizarParametroCommand, Unit>
{
    public async Task<Unit> Handle(
        ActualizarParametroCommand command,
        CancellationToken          ct)
    {
        var ahora    = DateTime.UtcNow;
        var username = currentUser.Username;

        var rows = await db.Database.ExecuteSqlAsync($"""
            UPDATE cfg.Parametros
            SET    Valor             = {command.Valor},
                   FechaActualizacion = {ahora},
                   ModificadoPor     = {username},
                   FechaModificacion = {ahora}
            WHERE  Clave = {command.Clave}
            """, ct);

        if (rows == 0)
            throw new EntidadNoEncontradaException("Parametro", command.Clave);

        // Invalidar caché para que el próximo ObtenerAsync lea de BD
        parametros.InvalidarCache(command.Clave);

        return Unit.Value;
    }
}
