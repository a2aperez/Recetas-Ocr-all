namespace RecetasOCR.Application.DTOs.Usuarios;

public record PermisoUsuarioDto(
    string Modulo,
    bool   PuedeLeer,
    bool   PuedeEscribir,
    bool   PuedeEliminar,
    bool   Denegado
);
