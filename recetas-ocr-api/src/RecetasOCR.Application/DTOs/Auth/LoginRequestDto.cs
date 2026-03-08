namespace RecetasOCR.Application.DTOs.Auth;

/// <summary>
/// Credenciales enviadas por el cliente en POST /api/auth/login.
/// </summary>
public record LoginRequestDto(
    string Username,
    string Password
);
