using FluentValidation;

namespace RecetasOCR.Application.Features.Imagenes;

/// <summary>
/// Valida SubirImagenCommand antes de que el handler ejecute.
/// Se activa en ValidationBehavior del pipeline MediatR.
/// Errores retornan HTTP 422 vía ExceptionHandlerMiddleware.
/// </summary>
public class SubirImagenCommandValidator : AbstractValidator<SubirImagenCommand>
{
    private static readonly string[] _origenesValidos =
        ["CAMARA", "GALERIA", "API", "ESCANER"];

    public SubirImagenCommandValidator()
    {
        RuleFor(x => x.IdGrupo)
            .NotEmpty()
            .WithMessage("El IdGrupo es obligatorio.");

        RuleFor(x => x.NombreArchivo)
            .NotEmpty()
            .WithMessage("El nombre de archivo es obligatorio.")
            .MaximumLength(200)
            .WithMessage("El nombre de archivo no puede exceder 200 caracteres.");

        RuleFor(x => x.OrigenImagen)
            .NotEmpty()
            .WithMessage("El origen de la imagen es obligatorio.")
            .Must(o => _origenesValidos.Contains(o, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"OrigenImagen debe ser uno de: {string.Join(", ", _origenesValidos)}.");
    }
}
