using FluentValidation;
using MediatR;

namespace RecetasOCR.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior que ejecuta todos los FluentValidation validators
/// registrados para el TRequest antes de llamar al handler.
/// Si hay errores lanza ValidationException con la lista completa.
/// El handler NUNCA se ejecuta si hay errores de validación.
/// ExceptionHandlerMiddleware captura ValidationException → HTTP 422.
/// Se ejecuta SEGUNDO en el pipeline (después de Logging, antes de Audit).
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
