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
    DateTime         FechaCreacion,
    bool             RequiereCambioPassword,
    List<PermisoDto> Permisos
);
