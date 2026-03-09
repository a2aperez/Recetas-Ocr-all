namespace RecetasOCR.Application.DTOs.Facturacion;

public record FacturaResumenDto(
    Guid      Id,
    string    UUID,
    string?   NombrePaciente,
    string    RFC,
    decimal   Total,
    DateOnly? FechaConsulta,
    DateTime? FechaTimbrado,
    string    Estado,
    string?   NombreAseguradora   // nullable: LEFT JOIN puede no encontrar aseguradora
);
