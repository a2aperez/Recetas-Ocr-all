using Microsoft.AspNetCore.Authorization;

namespace RecetasOCR.API.Authorization;

/// <summary>
/// Requisito de autorización basado en permiso de módulo.
/// Producido dinámicamente por PermisoPolicyProvider desde el nombre de la policy.
/// </summary>
public class PermisoRequirement : IAuthorizationRequirement
{
    /// <summary>Clave del módulo. Ej: "IMAGENES.SUBIR", "REVISION.APROBAR".</summary>
    public string Modulo { get; }

    /// <summary>Acción requerida: "leer", "escribir" o "eliminar".</summary>
    public string Accion { get; }

    public PermisoRequirement(string modulo, string accion)
    {
        Modulo = modulo;
        Accion = accion.ToLowerInvariant();
    }
}
