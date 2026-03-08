using FluentValidation;

namespace RecetasOCR.Application.Features.Auth;

/// <summary>
/// Validator para LoginCommand.
/// Se ejecuta en ValidationBehavior antes de que el handler corra.
/// Errores retornan HTTP 422 vía ExceptionHandlerMiddleware.
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("El nombre de usuario o email es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre de usuario no puede exceder 100 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.")
            .MaximumLength(200).WithMessage("La contraseña no puede exceder 200 caracteres.");
    }
}
