namespace RecetasOCR.Application.DTOs.Catalogos;

/// <summary>
/// DTO extendido para administración — incluye IdAseguradoraPadre y nombre del padre.
/// Retornado por GET /api/catalogos/aseguradoras cuando incluyeInactivas=true.
/// También retornado siempre para permitir construir el árbol jerárquico en el frontend.
/// </summary>
public record AseguradoraAdminDto(
    int     Id,
    string  Clave,
    string  Nombre,
    string? RazonSocial,       // columna NombreCorto en DB
    string? RFC,
    bool    Activo,
    int?    IdAseguradoraPadre,
    string? NombrePadre        // nombre de la aseguradora padre (LEFT JOIN)
);
