namespace RecetasOCR.Application.DTOs.Auth;

/// <summary>
/// Permiso efectivo por módulo, calculado combinando seg.PermisosRol y seg.PermisosUsuario.
/// PermisosUsuario con Denegado=1 sobreescribe al permiso del rol.
/// Alineado con PermisoUsuario del frontend (auth.types.ts).
/// </summary>
public record PermisoDto(
    string Modulo,
    bool   PuedeLeer,
    bool   PuedeEscribir,
    bool   PuedeEliminar
);
