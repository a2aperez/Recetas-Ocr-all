namespace RecetasOCR.Application.DTOs.Auth;

/// <summary>
/// Datos del usuario incluidos en la respuesta del login y en el payload del JWT.
/// Alineado con UsuarioSesion del frontend (auth.types.ts).
/// </summary>
public record UsuarioSesionDto(
    Guid             Id,
    string           Username,
    string           NombreCompleto,
    string           Email,
    string           Rol,
    List<PermisoDto> Permisos
);
