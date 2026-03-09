using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Infrastructure.Persistence;
using RecetasOCR.Infrastructure.Services;

namespace RecetasOCR.Infrastructure.Extensions;

/// <summary>
/// Registro de todos los servicios de Infrastructure en el DI container.
/// Llamar desde Program.cs: builder.Services.AddInfrastructureServices(configuration);
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF Core ─────────────────────────────────────────────────────────────
        services.AddDbContext<RecetasOcrDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("RecetasOCR"),
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount:       3,
                    maxRetryDelay:       TimeSpan.FromSeconds(5),
                    errorNumbersToAdd:   null)));

        // IRecetasOcrDbContext resuelve al DbContext concreto (mismo scope)
        services.AddScoped<IRecetasOcrDbContext>(
            sp => sp.GetRequiredService<RecetasOcrDbContext>());

        // ── Blob Storage ─────────────────────────────────────────────────────────
        services.AddScoped<IBlobStorageService, BlobStorageService>();

        // ── OCR API ──────────────────────────────────────────────────────────────
        // AddHttpClient gestiona el ciclo de vida del HttpClient correctamente
        services.AddHttpClient<IOcrApiService, NadroOcrApiService>();

        // ── JWT ──────────────────────────────────────────────────────────────────
        services.AddSingleton<IJwtService, JwtService>();

        // ── Parámetros (requiere IMemoryCache) ───────────────────────────────────
        services.AddMemoryCache();
        services.AddSingleton<IParametrosService, ParametrosService>();

        // ── Password Hasher (BCrypt Work Factor 11) ──────────────────────────────
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();

        // ── Email (stub — reemplazar con implementación real en producción) ───────
        services.AddScoped<IEmailService, EmailServiceStub>();

        // ── PAC (stub — reemplazar con implementación real del PAC en producción) ──
        services.AddScoped<IPacService, PacServiceStub>();

        // ── HttpClient "NadroOcrClient" con Polly ────────────────────────────────
        services.AddHttpClient("NadroOcrClient", client =>
        {
            client.BaseAddress = new Uri("https://concordia.nadro.dev");
            client.Timeout     = TimeSpan.FromSeconds(120);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    // ─── Polly policies ──────────────────────────────────────────────────────────

    /// <summary>
    /// 3 reintentos con backoff exponencial: 2 s / 4 s / 8 s.
    /// Solo reintenta en errores HTTP transitorios (5xx, timeout, red).
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    /// <summary>
    /// Circuit breaker: 5 fallos consecutivos → abre el circuito 30 s.
    /// Previene cascada de fallos hacia la API de Nadro.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
}
