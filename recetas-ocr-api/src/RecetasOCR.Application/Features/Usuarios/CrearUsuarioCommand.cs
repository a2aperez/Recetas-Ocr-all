using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Auth;
using RecetasOCR.Application.DTOs.Usuarios;

namespace RecetasOCR.Application.Features.Usuarios;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Crea un nuevo usuario con password temporal generado automáticamente.
/// El campo NombreCompleto puede incluir apellidos: se almacena tal cual.
/// IdRol corresponde al Id entero de seg.Roles (no Guid).
/// </summary>
public record CrearUsuarioCommand(
    string  Username,
    string  Email,
    string  NombreCompleto,
    int     IdRol
) : IRequest<CrearUsuarioResponseDto>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class CrearUsuarioCommandValidator : AbstractValidator<CrearUsuarioCommand>
{
    public CrearUsuarioCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200);

        RuleFor(x => x.NombreCompleto)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.IdRol)
            .GreaterThan(0);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class CrearUsuarioCommandHandler(
    IRecetasOcrDbContext   db,
    IPasswordHasherService passwordHasher,
    ICurrentUserService    currentUser)
    : IRequestHandler<CrearUsuarioCommand, CrearUsuarioResponseDto>
{
    public async Task<CrearUsuarioResponseDto> Handle(
        CrearUsuarioCommand command,
        CancellationToken   ct)
    {
        var usernameNorm = command.Username.Trim().ToLowerInvariant();
        var emailNorm    = command.Email.Trim().ToLowerInvariant();

        // ── Unicidad ──────────────────────────────────────────────────────────
        var usernameExiste = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value FROM seg.Usuarios
                WHERE LOWER(Username) = {usernameNorm}
                """)
            .FirstAsync(ct);

        if (usernameExiste > 0)
            throw new InvalidOperationException($"El username '{command.Username}' ya está en uso.");

        var emailExiste = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value FROM seg.Usuarios
                WHERE LOWER(Email) = {emailNorm}
                """)
            .FirstAsync(ct);

        if (emailExiste > 0)
            throw new InvalidOperationException($"El email '{command.Email}' ya está en uso.");

        // ── Password temporal ─────────────────────────────────────────────────
        var passwordTemporal = Guid.NewGuid().ToString("N")[..8].ToUpper();
        var passwordHash     = passwordHasher.Hash(passwordTemporal);

        var newId      = Guid.NewGuid();
        var creadoPor  = currentUser.Username ?? "sistema";

        // ── INSERT ────────────────────────────────────────────────────────────
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO seg.Usuarios
                (Id, Username, Email, PasswordHash, NombreCompleto,
                 IdRol, Activo, RequiereCambioPassword, IntentosFallidos,
                 FechaAlta, FechaActualizacion, CreadoPor, ModificadoPor, FechaModificacion)
            VALUES
                ({newId}, {command.Username}, {command.Email}, {passwordHash}, {command.NombreCompleto},
                 {command.IdRol}, 1, 1, 0,
                 GETUTCDATE(), GETUTCDATE(), {creadoPor}, {creadoPor}, GETUTCDATE())
            """, ct);

        // ── Auditoría ─────────────────────────────────────────────────────────
        var detalleLog = $"Usuario creado por {creadoPor}";
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO seg.LogAcceso (IdUsuario, Evento, Detalle, FechaEvento)
            VALUES ({newId}, 'USUARIO_CREADO', {detalleLog}, GETUTCDATE())
            """, ct);

        // ── Leer fila completa + permisos de rol ──────────────────────────────
        var row = await db.Database
            .SqlQuery<UsuarioDetalleRow>($"""
                SELECT u.Id, u.Username, u.Email, u.NombreCompleto,
                       r.Nombre AS NombreRol, u.Activo,
                       u.UltimoAcceso, u.FechaAlta AS FechaCreacion,
                       u.RequiereCambioPassword, u.IdRol
                FROM   seg.Usuarios u
                INNER  JOIN seg.Roles r ON r.Id = u.IdRol
                WHERE  u.Id = {newId}
                """)
            .FirstAsync(ct);

        var permisosRol = await db.Database
            .SqlQuery<GetUsuarioByIdQueryHandler.PermisoRow>($"""
                SELECT m.Clave AS Modulo,
                       pr.PuedeLeer, pr.PuedeEscribir, pr.PuedeEliminar,
                       CAST(0 AS BIT) AS Denegado
                FROM   seg.PermisosRol pr
                INNER  JOIN seg.Modulos m ON m.Id = pr.IdModulo
                WHERE  pr.IdRol = {command.IdRol}
                  AND  m.Activo = 1
                """)
            .ToListAsync(ct);

        var permisos = GetUsuarioByIdQueryHandler
            .CombinarPermisos(permisosRol, []);

        var detalle = new UsuarioDetalleDto(
            row.Id, row.Username, row.Email, row.NombreCompleto,
            row.NombreRol, row.Activo, row.UltimoAcceso, row.FechaCreacion,
            row.RequiereCambioPassword, permisos);

        return new CrearUsuarioResponseDto(detalle, passwordTemporal);
    }

    private sealed record UsuarioDetalleRow(
        Guid      Id,
        string    Username,
        string    Email,
        string    NombreCompleto,
        string    NombreRol,
        bool      Activo,
        DateTime? UltimoAcceso,
        DateTime  FechaCreacion,
        bool      RequiereCambioPassword,
        int       IdRol);
}
