namespace RecetasOCR.Application.DTOs.Usuarios;

public record UsuarioListaDto(
    Guid      Id,
    string    Username,
    string    Email,
    string    NombreCompleto,
    string    NombreRol,
    bool      Activo,
    DateTime? UltimoAcceso,
    DateTime  FechaAlta
);
