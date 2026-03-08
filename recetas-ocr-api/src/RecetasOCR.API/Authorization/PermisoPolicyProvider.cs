using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace RecetasOCR.API.Authorization;

/// <summary>
/// Proveedor dinámico de policies de autorización.
///
/// Formato de nombre de policy: "{MODULO}.{accion}"
/// El MODULO puede contener puntos (ej: "IMAGENES.SUBIR"), por eso se parte
/// en el ÚLTIMO punto para extraer la acción.
///
/// Ejemplos:
///   "IMAGENES.SUBIR.escribir"  → Modulo="IMAGENES.SUBIR",  Accion="escribir"
///   "REVISION.APROBAR.escribir"→ Modulo="REVISION.APROBAR", Accion="escribir"
///   "USUARIOS.ADMINISTRAR.leer"→ Modulo="USUARIOS.ADMINISTRAR", Accion="leer"
///
/// Policies sin sufijo de acción (leer|escribir|eliminar) se delegan al
/// DefaultAuthorizationPolicyProvider estándar.
/// </summary>
public class PermisoPolicyProvider : IAuthorizationPolicyProvider
{
    private static readonly HashSet<string> Acciones =
        new(StringComparer.OrdinalIgnoreCase) { "leer", "escribir", "eliminar" };

    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermisoPolicyProvider(IOptions<AuthorizationOptions> options)
        => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var lastDot = policyName.LastIndexOf('.');
        if (lastDot > 0)
        {
            var accion = policyName[(lastDot + 1)..];
            string modulo;

            if (Acciones.Contains(accion))
            {
                // Formato completo: "MODULO.accion" → ej: FACTURACION.GENERAR.escribir
                modulo = policyName[..lastDot];
            }
            else
            {
                // Sin sufijo de acción: ej: "USUARIOS.ADMINISTRAR", "FACTURACION.VER"
                // Tratar el nombre completo como módulo y asumir lectura.
                modulo = policyName;
                accion = "leer";
            }

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermisoRequirement(modulo, accion))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
