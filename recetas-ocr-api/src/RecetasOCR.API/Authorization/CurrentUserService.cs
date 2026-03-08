using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs;

namespace RecetasOCR.API.Authorization;

/// <summary>
/// Implementación de ICurrentUserService que lee IHttpContextAccessor.
/// Deserializa el claim "permisos" (JSON de List&lt;PermisoEfectivoDto&gt;)
/// para TienePermiso() con comprobación real de los bits PuedeLeer/Escribir/Eliminar.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(IHttpContextAccessor accessor, ILogger<CurrentUserService> logger)
    {
        _accessor = accessor;
        _logger   = logger;
    }

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
        var permisosJson = User?.FindFirstValue("permisos");
        if (string.IsNullOrWhiteSpace(permisosJson)) return false;

        try
        {
            var permisos = JsonSerializer.Deserialize<List<PermisoEfectivoDto>>(
                               permisosJson, JsonOpts);
            var permiso = permisos?.FirstOrDefault(p =>
                string.Equals(p.Modulo, modulo, StringComparison.OrdinalIgnoreCase));

            if (permiso is null) return false;

            return accion.ToLowerInvariant() switch
            {
                "leer"     => permiso.PuedeLeer,
                "escribir" => permiso.PuedeEscribir,
                "eliminar" => permiso.PuedeEliminar,
                _          => false
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "No se pudo deserializar el claim 'permisos' para módulo '{Modulo}'/acción '{Accion}'. Denegando acceso.",
                modulo, accion);
            return false;
        }
    }
}
