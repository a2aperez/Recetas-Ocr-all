using MediatR;
using RecetasOCR.Application.DTOs.Auth;

namespace RecetasOCR.Application.Features.Auth;

/// <summary>
/// Command para autenticar un usuario en el sistema.
/// El Username acepta nombre de usuario o email (case-insensitive).
/// No implementa IAuditableCommand: el login es anónimo,
/// no hay usuario previo que escribir en ModificadoPor.
/// </summary>
public record LoginCommand(
    string Username,
    string Password
) : IRequest<LoginResponseDto>;
