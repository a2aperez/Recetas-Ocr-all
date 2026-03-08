using MediatR;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior que se activa únicamente para Commands que implementen
/// IAuditableCommand (interfaz marker).
/// Verifica que ICurrentUserService.Username no sea null antes de que el
/// handler ejecute, garantizando que ModificadoPor siempre tenga valor.
/// Se ejecuta TERCERO en el pipeline (después de Validation, antes del Handler).
/// </summary>
public class AuditBehavior<TRequest, TResponse>(
    ICurrentUserService currentUser,
    ILogger<AuditBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Solo actúa sobre commands auditables
        if (request is not IAuditableCommand)
            return await next();

        var requestName = typeof(TRequest).Name;

        if (string.IsNullOrWhiteSpace(currentUser.Username))
        {
            logger.LogWarning(
                "[Audit] Command {RequestName} requiere usuario autenticado pero Username es null.",
                requestName);

            throw new PermisoInsuficienteException(
                modulo: requestName,
                accion: "ejecutar command auditable sin usuario autenticado");
        }

        logger.LogDebug(
            "[Audit] Command {RequestName} ejecutado por {Username}. ModificadoPor disponible.",
            requestName, currentUser.Username);

        return await next();
    }
}
