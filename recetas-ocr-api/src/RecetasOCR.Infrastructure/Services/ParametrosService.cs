using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Infrastructure.Persistence.Entities;

namespace RecetasOCR.Infrastructure.Services;

/// <summary>
/// Implementación de IParametrosService que lee de cfg.Parametros en BD.
/// Cachea cada clave individualmente durante 5 minutos (sliding expiration).
/// Usa IServiceScopeFactory para evitar capturar IRecetasOcrDbContext (Scoped)
/// en un Singleton — cada consulta a BD crea su propio scope efímero.
/// </summary>
public class ParametrosService : IParametrosService
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "param:";
    private const string CacheTotalKey  = "__parametros_all__";

    private readonly IServiceScopeFactory      _scopeFactory;
    private readonly IMemoryCache              _cache;
    private readonly ILogger<ParametrosService> _logger;

    public ParametrosService(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache,
        ILogger<ParametrosService> logger)
    {
        _scopeFactory = scopeFactory;
        _cache        = cache;
        _logger       = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────

    public async Task<string?> ObtenerAsync(string clave, CancellationToken ct = default)
    {
        var cacheKey = CacheKeyPrefix + clave;

        if (_cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        using var scope = _scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<IRecetasOcrDbContext>();

        var parametro = await ctx.Set<Parametro>()
            .AsNoTracking()
            .Where(p => p.Clave == clave)
            .Select(p => new { p.Valor })
            .FirstOrDefaultAsync(ct);

        if (parametro is null)
        {
            _logger.LogWarning(
                "Parámetro '{Clave}' no encontrado en cfg.Parametros. " +
                "Verifique que exista en la BD o en las seeds.", clave);

            // Cachear null brevemente para evitar golpear la BD en cada request
            _cache.Set(cacheKey, (string?)null,
                new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) });
            return null;
        }

        _cache.Set(cacheKey, parametro.Valor,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration,
                SlidingExpiration               = CacheExpiration
            });

        return parametro.Valor;
    }

    public async Task<decimal> ObtenerDecimalAsync(
        string clave, decimal defaultValue, CancellationToken ct = default)
    {
        var valor = await ObtenerAsync(clave, ct);

        if (valor is null)
            return defaultValue;

        if (decimal.TryParse(valor, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;

        _logger.LogWarning(
            "Parámetro '{Clave}' tiene valor '{Valor}' que no es un decimal válido. " +
            "Se usará el valor por defecto {Default}.", clave, valor, defaultValue);
        return defaultValue;
    }

    public async Task<int> ObtenerIntAsync(
        string clave, int defaultValue, CancellationToken ct = default)
    {
        var valor = await ObtenerAsync(clave, ct);

        if (valor is null)
            return defaultValue;

        if (int.TryParse(valor, out var result))
            return result;

        _logger.LogWarning(
            "Parámetro '{Clave}' tiene valor '{Valor}' que no es un entero válido. " +
            "Se usará el valor por defecto {Default}.", clave, valor, defaultValue);
        return defaultValue;
    }

    // ─── Invalidación de caché ────────────────────────────────────────────────

    public void InvalidarCache(string clave)
        => _cache.Remove(CacheKeyPrefix + clave);

    public void InvalidarCacheTotal()
    {
        // IMemoryCache no expone enumeración de claves; usamos un token de cancelación
        // registrado en cada entrada para invalidación masiva.
        // Implementación pragmática: reiniciamos el token compartido.
        if (_cache is MemoryCache mc)
        {
            mc.Clear();
        }
        else
        {
            // Fallback: eliminar la clave centinela — los entries individuales
            // expirarán naturalmente o serán invalidados individualmente.
            _cache.Remove(CacheTotalKey);
            _logger.LogWarning(
                "InvalidarCacheTotal: IMemoryCache no es MemoryCache concreto. " +
                "Las entradas expirarán de forma natural en {Min} min.", CacheExpiration.TotalMinutes);
        }
    }
}
