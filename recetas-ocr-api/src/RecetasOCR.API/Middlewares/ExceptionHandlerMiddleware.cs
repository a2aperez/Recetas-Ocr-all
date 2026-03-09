using System.Security.Claims;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.API.Middlewares;

/// <summary>
/// Captura todas las excepciones no manejadas y devuelve ApiResponse&lt;object&gt; estándar.
/// Stack trace solo se incluye en Development. Nunca en producción.
/// </summary>
public class ExceptionHandlerMiddleware(
    RequestDelegate                    next,
    ILogger<ExceptionHandlerMiddleware> logger,
    IWebHostEnvironment                 env)
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var path     = context.Request.Path.Value ?? "/";
        var username = context.User.FindFirstValue("username")
                    ?? context.User.FindFirstValue(ClaimTypes.Name)
                    ?? "anónimo";

        var (status, errors, logLevel) = ex switch
        {
            ValidationException ve           => (400, ve.Errors.Select(e => e.ErrorMessage).ToList(), LogLevel.Warning),
            EntidadNoEncontradaException     => (404, new List<string> { ex.Message },                LogLevel.Warning),
            UnauthorizedAccessException      => (401, new List<string> { ex.Message },                LogLevel.Warning),
            PermisoInsuficienteException     => (403, new List<string> { ex.Message },                LogLevel.Warning),
            EstadoInvalidoException          => (409, new List<string> { ex.Message },                LogLevel.Warning),
            ImagenIlegibleException          => (422, new List<string> { ex.Message },                LogLevel.Warning),
            GrupoNoFacturableException       => (422, new List<string> { ex.Message },                LogLevel.Warning),
            BlobStorageException             => (502, new List<string> { "Error al acceder al almacenamiento de archivos." }, LogLevel.Error),
            _                               => (500, new List<string> { "Error interno del servidor." }, LogLevel.Error),
        };

        logger.Log(logLevel, ex,
            "[{Status}] {ExType} — Path: {Path} | User: {Username} | {Message}",
            status, ex.GetType().Name, path, username, ex.Message);

        // Log explícito de InnerExceptions para facilitar diagnóstico en desarrollo
        var inner = ex.InnerException;
        var depth = 1;
        while (inner != null)
        {
            logger.LogError(
                "  [{Depth}] InnerException {InnerType}: {InnerMessage}\n{StackTrace}",
                depth, inner.GetType().Name, inner.Message, inner.StackTrace);
            inner = inner.InnerException;
            depth++;
        }

        var response = ApiResponse<object>.Fail(errors);

        // Include detail only in development
        if (env.IsDevelopment() && status == 500)
            response = new ApiResponse<object>
            {
                Success = false,
                Errors  = errors,
                Message = BuildDevMessage(ex)
            };

        context.Response.StatusCode  = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOpts));
    }

    private static string BuildDevMessage(Exception ex)
    {
        var sb      = new System.Text.StringBuilder();
        var current = ex;
        var depth   = 0;
        while (current != null)
        {
            var prefix = depth == 0 ? "" : new string(' ', depth * 2) + "↳ ";
            sb.AppendLine($"{prefix}{current.GetType().Name}: {current.Message}");
            if (current.StackTrace is not null)
            {
                foreach (var line in current.StackTrace.Split('\n'))
                    sb.AppendLine($"{prefix}  {line.TrimEnd()}");
            }
            current = current.InnerException;
            depth++;
        }
        return sb.ToString();
    }
}
