namespace RecetasOCR.Domain.Entities;

/// <summary>
/// Clase base para todas las entidades operativas.
/// ModificadoPor y FechaModificacion se actualizan en cada escritura
/// mediante SetAuditoria(), llamado desde los handlers de Application.
/// El valor de ModificadoPor viene de ICurrentUserService.Username.
/// </summary>
public abstract class AuditableEntity
{
    public string? ModificadoPor { get; set; }
    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Asigna el usuario y la fecha UTC de la última modificación.
    /// Llamar en cada Command handler antes de persistir.
    /// </summary>
    public void SetAuditoria(string username)
    {
        ModificadoPor = username;
        FechaModificacion = DateTime.UtcNow;
    }
}
