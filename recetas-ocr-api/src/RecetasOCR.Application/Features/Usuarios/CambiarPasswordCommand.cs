using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Usuarios;

// ── Command ───────────────────────────────────────────────────────────────────

public record CambiarPasswordCommand(
    Guid   IdUsuario,
    string PasswordActual,
    string PasswordNuevo,
    string PasswordConfirmacion
) : IRequest<bool>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class CambiarPasswordCommandValidator : AbstractValidator<CambiarPasswordCommand>
{
    public CambiarPasswordCommandValidator()
    {
        RuleFor(x => x.PasswordActual)
            .NotEmpty();

        RuleFor(x => x.PasswordNuevo)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(x => x.PasswordConfirmacion)
            .NotEmpty()
            .Equal(x => x.PasswordNuevo)
                .WithMessage("La confirmación de contraseña no coincide.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class CambiarPasswordCommandHandler(
    IRecetasOcrDbContext   db,
    IPasswordHasherService passwordHasher,
    ICurrentUserService    currentUser)
    : IRequestHandler<CambiarPasswordCommand, bool>
{
    public async Task<bool> Handle(
        CambiarPasswordCommand command,
        CancellationToken      ct)
    {
        // Solo el propio usuario puede cambiar su contraseña
        if (currentUser.UserId != command.IdUsuario)
            throw new UnauthorizedAccessException(
                "Solo puedes cambiar tu propia contraseña.");

        var hashRow = await db.Database
            .SqlQuery<PasswordRow>($"""
                SELECT PasswordHash FROM seg.Usuarios WHERE Id = {command.IdUsuario}
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntidadNoEncontradaException("Usuario", command.IdUsuario);

        if (!passwordHasher.Verificar(command.PasswordActual, hashRow.PasswordHash))
            throw new InvalidOperationException("La contraseña actual es incorrecta.");

        var nuevoHash = passwordHasher.Hash(command.PasswordNuevo);

        await db.Database.ExecuteSqlAsync($"""
            UPDATE seg.Usuarios
            SET    PasswordHash           = {nuevoHash},
                   RequiereCambioPassword = 0,
                   FechaActualizacion     = GETUTCDATE(),
                   FechaModificacion      = GETUTCDATE()
            WHERE  Id = {command.IdUsuario}
            """, ct);

        return true;
    }

    private sealed record PasswordRow(string PasswordHash);
}
