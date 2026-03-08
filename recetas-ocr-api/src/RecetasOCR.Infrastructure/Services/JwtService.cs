using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs;

namespace RecetasOCR.Infrastructure.Services;

/// <summary>
/// Implementación de IJwtService con Microsoft.IdentityModel.Tokens.
///
/// Configuración esperada en appsettings.json:
/// "Jwt": {
///   "SecretKey":          "...",   (mínimo 32 chars para HMAC-SHA256)
///   "ExpirationMinutes":  60,
///   "Issuer":             "RecetasOCR-API",
///   "Audience":           "RecetasOCR-SPA"
/// }
///
/// Claims emitidos:
///   sub        → userId (Guid)
///   username   → Username
///   rol        → Rol
///   permisos   → JSON de List&lt;PermisoEfectivoDto&gt;
///   jti        → Guid.NewGuid() (para revocación individual en seg.Sesiones)
/// </summary>
public class JwtService : IJwtService
{
    private const string ClaimUsername = "username";
    private const string ClaimRol      = "rol";
    private const string ClaimPermisos = "permisos";

    private readonly string   _secretKey;
    private readonly int      _expirationMinutes;
    private readonly string   _issuer;
    private readonly string   _audience;

    public JwtService(IConfiguration configuration)
    {
        var section = configuration.GetSection("Jwt");

        _secretKey         = section["SecretKey"]
                             ?? throw new InvalidOperationException("Falta Jwt:SecretKey en configuración.");
        _expirationMinutes = int.TryParse(section["ExpirationMinutes"], out var min) ? min : 60;
        _issuer            = section["Issuer"]   ?? "RecetasOCR-API";
        _audience          = section["Audience"] ?? "RecetasOCR-SPA";
    }

    // ─── GenerarToken ─────────────────────────────────────────────────────────

    public string GenerarToken(UsuarioDto usuario)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var permisosJson = JsonSerializer.Serialize(usuario.Permisos);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,  usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
            new Claim(ClaimUsername,                usuario.Username),
            new Claim(ClaimRol,                     usuario.Rol),
            new Claim(ClaimPermisos,                permisosJson)
        };

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ─── GenerarRefreshToken ──────────────────────────────────────────────────

    /// <summary>
    /// Opaque refresh token: GUID compacto + timestamp UTC en Base64.
    /// Formato: {guid32chars}{base64Timestamp}  ≈ 53 caracteres, URL-safe si se necesita.
    /// </summary>
    public string GenerarRefreshToken()
    {
        var guidPart      = Guid.NewGuid().ToString("N");                       // 32 chars hex
        var timestampBytes = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
        var tsPart        = Convert.ToBase64String(timestampBytes);             // ≈ 12 chars
        return guidPart + tsPart;
    }

    // ─── ObtenerClaimsDeToken ─────────────────────────────────────────────────

    /// <summary>
    /// Extrae claims de un JWT aunque haya expirado.
    /// Retorna null si la firma es inválida o el token está malformado.
    /// </summary>
    public ClaimsPrincipal? ObtenerClaimsDeToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = key,
            ValidateIssuer           = true,
            ValidIssuer              = _issuer,
            ValidateAudience         = true,
            ValidAudience            = _audience,
            ValidateLifetime         = false     // permite tokens expirados (flujo refresh)
        };

        try
        {
            var handler    = new JwtSecurityTokenHandler();
            var principal  = handler.ValidateToken(token, validationParams, out _);
            return principal;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }

    // ─── ValidarRefreshToken ──────────────────────────────────────────────────

    /// <summary>
    /// Valida únicamente el formato del refresh token (GUID 32 chars + Base64 timestamp).
    /// La validación de expiración y revocación se realiza consultando seg.Sesiones en BD.
    /// </summary>
    public bool ValidarRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length < 32)
            return false;

        // Los primeros 32 caracteres deben ser hex válido (GUID sin guiones)
        var guidPart = refreshToken[..32];
        return Guid.TryParseExact(guidPart, "N", out _);
    }
}
