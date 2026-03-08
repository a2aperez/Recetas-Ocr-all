namespace RecetasOCR.Application.Common.Interfaces;

/// <summary>
/// Abstracción para hashing y verificación de contraseñas.
/// Implementada en Infrastructure con BCrypt.Net-Next (Work Factor 12).
/// Application no depende directamente de ninguna librería de hashing.
/// </summary>
public interface IPasswordHasherService
{
    /// <summary>
    /// Genera el hash BCrypt de una contraseña en texto plano.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Verifica si una contraseña en texto plano corresponde al hash almacenado.
    /// </summary>
    bool Verificar(string password, string hash);
}
