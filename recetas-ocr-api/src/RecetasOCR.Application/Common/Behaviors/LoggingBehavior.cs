using MediatR;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using System.Diagnostics;

namespace RecetasOCR.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior que logea inicio, fin, duración y resultado
/// de cada Command y Query que pasa por MediatR.
/// Se ejecuta PRIMERO en el pipeline (antes de Validation y Audit).
/// </summary>
public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var username    = currentUser.Username ?? "anonimo";
        var sw          = Stopwatch.StartNew();

        logger.LogInformation(
            "[MediatR] Iniciando {RequestName} | Usuario: {Username}",
            requestName, username);

        try
        {
            var response = await next();
            sw.Stop();

            logger.LogInformation(
                "[MediatR] Completado {RequestName} | Usuario: {Username} | Duración: {ElapsedMs}ms",
                requestName, username, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();

            logger.LogError(ex,
                "[MediatR] Error en {RequestName} | Usuario: {Username} | Duración: {ElapsedMs}ms | Error: {ErrorMessage}",
                requestName, username, sw.ElapsedMilliseconds, ex.Message);

            throw;
        }
    }
}
