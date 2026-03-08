namespace RecetasOCR.API.Middlewares;

/// <summary>
/// Extrae el username del JWT y lo pone disponible via ICurrentUserService
/// para que los handlers lo usen al escribir ModificadoPor en la BD.
/// </summary>
public class AuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // ICurrentUserService se resuelve del DI y lee context.User.Claims
        // El middleware solo asegura que el contexto HTTP esté disponible
        await next(context);
    }
}
