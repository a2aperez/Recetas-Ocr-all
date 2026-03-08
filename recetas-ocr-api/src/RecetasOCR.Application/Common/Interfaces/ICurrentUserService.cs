namespace RecetasOCR.Application.Common.Interfaces;

/// <summary>
/// Expone los datos del usuario autenticado en el request actual.
/// Se inyecta en los handlers para poblar ModificadoPor en cada escritura.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Rol { get; }
    bool TienePermiso(string modulo, string accion);
}
