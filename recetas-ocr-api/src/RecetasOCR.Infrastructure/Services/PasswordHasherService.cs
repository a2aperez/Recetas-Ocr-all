using RecetasOCR.Application.Common.Interfaces;

namespace RecetasOCR.Infrastructure.Services;

/// <summary>
/// Implementación de IPasswordHasherService usando BCrypt.Net-Next.
/// Work Factor 11 — equilibrio seguridad/rendimiento para este dominio.
/// </summary>
public class PasswordHasherService : IPasswordHasherService
{
    private const int WorkFactor = 11;

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verificar(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
