using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RecetasOCR.Application.Common.Behaviors;
using RecetasOCR.Application.Features.Auth;

namespace RecetasOCR.Application;

/// <summary>
/// Registro de todos los servicios de la capa Application en el DI container.
/// Llamar desde Program.cs: builder.Services.AddApplicationServices();
///
/// Orden de behaviors en el pipeline MediatR:
///   1. LoggingBehavior  → mide duración total, logea inicio/fin/error
///   2. ValidationBehavior → aborta si FluentValidation falla (HTTP 422)
///   3. AuditBehavior    → verifica usuario en IAuditableCommand
///   4. Handler          → lógica de negocio
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // AutoMapper — escanea perfiles en este assembly
        services.AddAutoMapper(assembly);

        // FluentValidation — registra todos los validators del assembly
        services.AddValidatorsFromAssembly(assembly);

        // MediatR + pipeline behaviors en orden de ejecución
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // 1. Logging — primero para medir duración total del pipeline
            cfg.AddBehavior(typeof(IPipelineBehavior<,>),
                            typeof(LoggingBehavior<,>));

            // 2. Validation — antes del handler, después del log de inicio
            cfg.AddBehavior(typeof(IPipelineBehavior<,>),
                            typeof(ValidationBehavior<,>));

            // 3. Audit — verifica usuario autenticado en IAuditableCommand
            cfg.AddBehavior(typeof(IPipelineBehavior<,>),
                            typeof(AuditBehavior<,>));
        });

        // ── Auth data access (mockeable en tests) ────────────────────────────
        services.AddScoped<ILoginQueryRepository, LoginQueryRepository>();

        return services;
    }
}
