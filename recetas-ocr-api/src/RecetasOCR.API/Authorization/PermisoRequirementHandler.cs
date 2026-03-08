using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Infrastructure.Persistence.Entities;

namespace RecetasOCR.API.Authorization;

/// <summary>
/// Handler que evalúa PermisoRequirement en dos etapas:
///   1. Claim "permisos" del JWT (fast path — sin BD)
///   2. Fallback a seg.PermisosUsuario + seg.PermisosRol (BD)
///
/// Regla inviolable: si PermisosUsuario.Denegado = 1 → context.Fail() siempre,
/// independientemente de lo que diga el claim del JWT.
///
/// Registrar como Scoped (IAuthorizationHandler) en DI para
/// poder resolver IRecetasOcrDbContext por request.
/// </summary>
public class PermisoRequirementHandler : AuthorizationHandler<PermisoRequirement>
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PermisoRequirementHandler> _logger;

    public PermisoRequirementHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<PermisoRequirementHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermisoRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? context.User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var userId))
            return;

        // ── PASO 1: JWT claim (fast path) ────────────────────────────────────
        var permisosJson = context.User.FindFirstValue("permisos");
        if (!string.IsNullOrWhiteSpace(permisosJson))
        {
            try
            {
                var permisos = JsonSerializer.Deserialize<List<PermisoEfectivoDto>>(
                                   permisosJson, JsonOpts);
                var p = permisos?.FirstOrDefault(x =>
                    string.Equals(x.Modulo, requirement.Modulo,
                                  StringComparison.OrdinalIgnoreCase));

                if (p is not null && TieneAcceso(p.PuedeLeer, p.PuedeEscribir, p.PuedeEliminar,
                                                 requirement.Accion))
                {
                    // Aún con JWT OK, verificar Denegado en BD para detectar revocaciones
                    // ocurridas DESPUÉS de la emisión del token.
                    bool denegado = await IsDenegadoEnBdAsync(userId, requirement.Modulo);
                    if (denegado)
                    {
                        context.Fail(new AuthorizationFailureReason(this,
                            $"Acceso denegado explícitamente al módulo '{requirement.Modulo}'."));
                        return;
                    }

                    context.Succeed(requirement);
                    return;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo deserializar claim 'permisos' en PermisoRequirementHandler.");
            }
        }

        // ── PASO 2: Fallback a BD ────────────────────────────────────────────
        await EvaluarDesdeBdAsync(context, requirement, userId);
    }

    // ─── BD Helpers ──────────────────────────────────────────────────────────

    private async Task<bool> IsDenegadoEnBdAsync(Guid userId, string moduloClave)
    {
        using var scope = _scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<IRecetasOcrDbContext>();

        return await ctx.Set<PermisosUsuario>()
            .AsNoTracking()
            .AnyAsync(p =>
                p.IdUsuario == userId &&
                p.Denegado  == true &&
                p.IdModuloNavigation.Clave == moduloClave);
    }

    private async Task EvaluarDesdeBdAsync(
        AuthorizationHandlerContext context,
        PermisoRequirement          requirement,
        Guid                        userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<IRecetasOcrDbContext>();

        // Obtener ID del módulo
        var moduloId = await ctx.Set<Modulo>()
            .AsNoTracking()
            .Where(m => m.Clave == requirement.Modulo && m.Activo)
            .Select(m => (int?)m.Id)
            .FirstOrDefaultAsync();

        if (moduloId is null)
            return;

        // 1) Permiso explícito del usuario
        var permisoUsuario = await ctx.Set<PermisosUsuario>()
            .AsNoTracking()
            .Where(p => p.IdUsuario == userId && p.IdModulo == moduloId)
            .FirstOrDefaultAsync();

        if (permisoUsuario is not null)
        {
            if (permisoUsuario.Denegado)
            {
                context.Fail(new AuthorizationFailureReason(this,
                    $"Acceso denegado explícitamente al módulo '{requirement.Modulo}'."));
                return;
            }

            if (TieneAcceso(permisoUsuario.PuedeLeer, permisoUsuario.PuedeEscribir,
                            permisoUsuario.PuedeEliminar, requirement.Accion))
            {
                context.Succeed(requirement);
                return;
            }
        }

        // 2) Permiso del rol del usuario
        var idRol = await ctx.Set<Usuario>()
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => (int?)u.IdRol)
            .FirstOrDefaultAsync();

        if (idRol is null)
            return;

        var permisoRol = await ctx.Set<PermisosRol>()
            .AsNoTracking()
            .Where(p => p.IdRol == idRol && p.IdModulo == moduloId)
            .FirstOrDefaultAsync();

        if (permisoRol is not null &&
            TieneAcceso(permisoRol.PuedeLeer, permisoRol.PuedeEscribir,
                        permisoRol.PuedeEliminar, requirement.Accion))
        {
            context.Succeed(requirement);
        }
    }

    private static bool TieneAcceso(bool leer, bool escribir, bool eliminar, string accion) =>
        accion switch
        {
            "leer"     => leer,
            "escribir" => escribir,
            "eliminar" => eliminar,
            _          => false
        };
}
