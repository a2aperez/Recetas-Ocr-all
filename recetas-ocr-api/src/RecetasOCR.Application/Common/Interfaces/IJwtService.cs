using System.Security.Claims;
using RecetasOCR.Application.DTOs;

namespace RecetasOCR.Application.Common.Interfaces;

/// <summary>
/// Servicio de generación y validación de tokens JWT y Refresh Tokens.
/// Implementado en Infrastructure.Services.JwtService.
/// Lee configuración de appsettings.json → sección "Jwt".
/// Los tokens generados se persisten en seg.Sesiones (JwtTokenId + RefreshToken).
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Genera un token JWT firmado con los claims del usuario.
    /// El jti claim debe guardarse en seg.Sesiones.JwtTokenId para soporte
    /// de revocación por token individual.
    /// </summary>
    /// <param name="usuario">DTO con Id, Username, Rol y permisos efectivos.</param>
    /// <returns>Token JWT serializado como string.</returns>
    string GenerarToken(UsuarioDto usuario);

    /// <summary>
    /// Genera un Refresh Token opaco (GUID + timestamp en Base64URL).
    /// Debe persistirse en seg.Sesiones.RefreshToken con su fecha de expiración.
    /// </summary>
    string GenerarRefreshToken();

    /// <summary>
    /// Extrae y valida los claims de un token JWT, incluso si está expirado.
    /// Se usa en el flujo de refresh para verificar el jti del token anterior.
    /// Retorna null si el token tiene firma inválida o está malformado.
    /// </summary>
    /// <param name="token">Token JWT (puede estar expirado).</param>
    ClaimsPrincipal? ObtenerClaimsDeToken(string token);

    /// <summary>
    /// Verifica que el Refresh Token tenga el formato esperado.
    /// La validación de expiración y revocación se hace contra seg.Sesiones en BD.
    /// </summary>
    bool ValidarRefreshToken(string refreshToken);
}
