namespace RecetasOCR.Application.Common.Interfaces;

/// <summary>
/// Interfaz marker para Commands que modifican entidades en la BD.
/// AuditBehavior detecta cualquier IRequest que implemente esta interfaz
/// y garantiza que ModificadoPor esté disponible antes de ejecutar el handler.
///
/// Uso:
///   public record SubirImagenCommand(...) : IRequest&lt;ImagenDto&gt;, IAuditableCommand;
/// </summary>
public interface IAuditableCommand;
