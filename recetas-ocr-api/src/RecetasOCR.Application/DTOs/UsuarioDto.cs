namespace RecetasOCR.Application.DTOs;

/// <summary>
/// DTO con los datos del usuario autenticado usados para generar el JWT
/// y poblar ICurrentUserService en cada request.
/// Construido desde seg.Usuarios + seg.Roles + seg.PermisosRol + seg.PermisosUsuario.
/// </summary>
public record UsuarioDto(
    Guid Id,
    string Username,
    string NombreCompleto,
    string Email,
    string Rol,
    IReadOnlyList<PermisoEfectivoDto> Permisos
);

/// <summary>
/// Permiso efectivo calculado combinando seg.PermisosRol y seg.PermisosUsuario.
/// El permiso individual (PermisosUsuario) sobreescribe al del rol cuando existe.
/// </summary>
public record PermisoEfectivoDto(
    string Modulo,
    bool PuedeLeer,
    bool PuedeEscribir,
    bool PuedeEliminar
);
