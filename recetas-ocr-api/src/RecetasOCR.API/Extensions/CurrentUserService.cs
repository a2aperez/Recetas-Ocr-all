using System.Security.Claims;
using RecetasOCR.Application.Common.Interfaces;

namespace RecetasOCR.API.Extensions;

/// <summary>
/// Implementación de ICurrentUserService que lee el ClaimsPrincipal
/// del HttpContext del request actual (IHttpContextAccessor).
/// Registrar como Scoped junto con AddHttpContextAccessor().
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
        => _accessor = accessor;

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Username => User?.FindFirstValue("username")
                            ?? User?.FindFirstValue(ClaimTypes.Name);

    public string? Rol => User?.FindFirstValue("rol")
                       ?? User?.FindFirstValue(ClaimTypes.Role);

    public bool TienePermiso(string modulo, string accion)
    {
        // Los permisos detallados se validan a nivel de handler/policy;
        // aquí exponemos solo si el usuario tiene el claim de permisos.
        var permisosJson = User?.FindFirstValue("permisos");
        if (string.IsNullOrWhiteSpace(permisosJson)) return false;

        // Búsqueda simple de string para evitar deserialización en cada check.
        return permisosJson.Contains($"\"{modulo}\"", StringComparison.OrdinalIgnoreCase);
    }
}
