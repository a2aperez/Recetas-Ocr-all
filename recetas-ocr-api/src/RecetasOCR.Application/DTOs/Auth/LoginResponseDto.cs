namespace RecetasOCR.Application.DTOs.Auth;

/// <summary>
/// Respuesta del login exitoso.
/// Token y RefreshToken se persisten en seg.Sesiones.
/// El cliente los almacena en sessionStorage (nunca localStorage).
/// </summary>
public record LoginResponseDto(
    string          Token,
    string          RefreshToken,
    DateTime        ExpiraEn,
    UsuarioSesionDto Usuario
);
