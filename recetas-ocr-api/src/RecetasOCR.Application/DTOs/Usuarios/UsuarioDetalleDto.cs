using RecetasOCR.Application.DTOs.Auth;

namespace RecetasOCR.Application.DTOs.Usuarios;

public record UsuarioDetalleDto(
    Guid             Id,
    string           Username,
    string           Email,
    string           NombreCompleto,
    string           NombreRol,
    bool             Activo,
    DateTime?        UltimoAcceso,
    DateTime         FechaAlta,
    bool             RequiereCambioPassword,
    List<PermisoDto> Permisos,
    int              IdRol
);
