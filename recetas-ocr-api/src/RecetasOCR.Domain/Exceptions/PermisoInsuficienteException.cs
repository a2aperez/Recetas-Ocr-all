namespace RecetasOCR.Domain.Exceptions;

/// <summary>
/// Se lanza cuando el usuario autenticado no tiene permiso suficiente
/// para ejecutar una acción en un módulo.
/// El permiso individual (seg.PermisosUsuario) puede denegar aunque el rol lo permita.
/// </summary>
public class PermisoInsuficienteException : Exception
{
    public string Modulo { get; }
    public string Accion { get; }

    public PermisoInsuficienteException(string modulo, string accion)
        : base($"Sin permiso para {accion} en módulo {modulo}.")
    {
        Modulo = modulo;
        Accion = accion;
    }
}