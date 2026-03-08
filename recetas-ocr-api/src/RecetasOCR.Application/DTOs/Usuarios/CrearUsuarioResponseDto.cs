namespace RecetasOCR.Application.DTOs.Usuarios;

public record CrearUsuarioResponseDto(
    UsuarioDetalleDto Usuario,
    string            PasswordTemporal
);
