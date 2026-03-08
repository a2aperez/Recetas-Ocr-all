using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Auth;

namespace RecetasOCR.Application.Features.Auth;

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResponseDto>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("El refresh token no puede estar vacío.")
            .MaximumLength(500).WithMessage("El refresh token excede la longitud máxima permitida.");
    }
}

public class RefreshTokenCommandHandler(
    IRecetasOcrDbContext                db,
    IJwtService                         jwtService,
    IParametrosService                  parametros,
    ILogger<RefreshTokenCommandHandler> logger)
    : IRequestHandler<RefreshTokenCommand, LoginResponseDto>
{
    public async Task<LoginResponseDto> Handle(
        RefreshTokenCommand request,
        CancellationToken   ct)
    {
        // 1. Validate format
        if (!jwtService.ValidarRefreshToken(request.RefreshToken))
            throw new UnauthorizedAccessException("RefreshToken inválido.");

        // 2. Look up active session in BD
        var sesion = await db.Database
            .SqlQuery<SesionRow>($"""
                SELECT s.Id, s.IdUsuario, s.JwtTokenId, s.Estado, s.FechaExpiracion,
                       u.Username, u.NombreCompleto, u.Email,
                       r.Clave AS RolClave
                FROM   seg.Sesiones s
                INNER JOIN seg.Usuarios u ON u.Id = s.IdUsuario
                INNER JOIN seg.Roles    r ON r.Id = u.IdRol
                WHERE  s.RefreshToken = {request.RefreshToken}
                  AND  s.Estado       = 'ACTIVA'
                  AND  u.Activo       = 1
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("Sesión no encontrada o expirada.");

        if (sesion.FechaExpiracion < DateTime.UtcNow)
        {
            await RevocarAsync(sesion.Id, "EXPIRADA", ct);
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Sesión expirada.");
        }

        // 3. Recalculate effective permissions
        var permisosRol = await db.Database
            .SqlQuery<PermisoRow>($"""
                SELECT m.Clave AS ModuloClave,
                       pr.PuedeLeer, pr.PuedeEscribir, pr.PuedeEliminar,
                       CAST(0 AS BIT) AS Denegado
                FROM   seg.PermisosRol pr
                INNER JOIN seg.Modulos m ON m.Id = pr.IdModulo
                WHERE  pr.IdRol = (SELECT IdRol FROM seg.Usuarios WHERE Id = {sesion.IdUsuario})
                  AND  m.Activo = 1
                """)
            .ToListAsync(ct);

        var permisosUsr = await db.Database
            .SqlQuery<PermisoRow>($"""
                SELECT m.Clave AS ModuloClave,
                       pu.PuedeLeer, pu.PuedeEscribir, pu.PuedeEliminar,
                       pu.Denegado
                FROM   seg.PermisosUsuario pu
                INNER JOIN seg.Modulos m ON m.Id = pu.IdModulo
                WHERE  pu.IdUsuario = {sesion.IdUsuario}
                  AND  m.Activo     = 1
                """)
            .ToListAsync(ct);

        var usrIdx = permisosUsr.ToDictionary(p => p.ModuloClave);
        var permisosEfectivos = new List<PermisoDto>();
        var modulos = permisosRol.Select(p => p.ModuloClave)
            .Union(permisosUsr.Select(p => p.ModuloClave))
            .Distinct();

        foreach (var modulo in modulos)
        {
            usrIdx.TryGetValue(modulo, out var pu);
            if (pu is { Denegado: true }) continue;
            var pr = permisosRol.FirstOrDefault(p => p.ModuloClave == modulo);
            permisosEfectivos.Add(new PermisoDto(
                Modulo:        modulo,
                PuedeLeer:     pu?.PuedeLeer     ?? pr?.PuedeLeer     ?? false,
                PuedeEscribir: pu?.PuedeEscribir ?? pr?.PuedeEscribir ?? false,
                PuedeEliminar: pu?.PuedeEliminar ?? pr?.PuedeEliminar ?? false
            ));
        }

        // 4. Generate new tokens
        var usuarioDto = new UsuarioDto(
            Id:             sesion.IdUsuario,
            Username:       sesion.Username,
            NombreCompleto: sesion.NombreCompleto,
            Email:          sesion.Email,
            Rol:            sesion.RolClave,
            Permisos:       permisosEfectivos
                .Select(p => new PermisoEfectivoDto(p.Modulo, p.PuedeLeer, p.PuedeEscribir, p.PuedeEliminar))
                .ToList()
                .AsReadOnly()
        );

        var jwtExpMin    = await parametros.ObtenerIntAsync("JWT_EXPIRACION_MINUTOS", 60, ct);
        var nuevoToken   = jwtService.GenerarToken(usuarioDto);
        var nuevoRefresh = jwtService.GenerarRefreshToken();
        var expiraEn     = DateTime.UtcNow.AddMinutes(jwtExpMin);
        var claims       = jwtService.ObtenerClaimsDeToken(nuevoToken);
        var jwtTokenId   = claims?.FindFirst("jti")?.Value ?? Guid.NewGuid().ToString();

        // 5. Revoke old session, insert new one
        await RevocarAsync(sesion.Id, "RENOVADA", ct);

        var nuevaSesionId = Guid.NewGuid();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO seg.Sesiones
                (Id, IdUsuario, JwtTokenId, RefreshToken,
                 FechaInicio, FechaExpiracion, FechaUltimaActividad, Estado)
            VALUES
                ({nuevaSesionId}, {sesion.IdUsuario}, {jwtTokenId}, {nuevoRefresh},
                 GETUTCDATE(), {expiraEn}, GETUTCDATE(), 'ACTIVA')
            """, ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Auth] TOKEN_RENOVADO — {Username} | Sesión: {SesionId}",
            sesion.Username, nuevaSesionId);

        return new LoginResponseDto(
            Token:        nuevoToken,
            RefreshToken: nuevoRefresh,
            ExpiraEn:     expiraEn,
            Usuario: new UsuarioSesionDto(
                Id:             sesion.IdUsuario,
                Username:       sesion.Username,
                NombreCompleto: sesion.NombreCompleto,
                Email:          sesion.Email,
                Rol:            sesion.RolClave,
                Permisos:       permisosEfectivos
            )
        );
    }

    private Task RevocarAsync(Guid sesionId, string motivo, CancellationToken ct) =>
        db.Database.ExecuteSqlAsync($"""
            UPDATE seg.Sesiones
            SET    Estado = 'CERRADA', MotivoRevocacion = {motivo}
            WHERE  Id = {sesionId}
            """, ct);

    private sealed record SesionRow(
        Guid     Id,
        Guid     IdUsuario,
        string   JwtTokenId,
        string   Estado,
        DateTime FechaExpiracion,
        string   Username,
        string   NombreCompleto,
        string   Email,
        string   RolClave
    );

    private sealed record PermisoRow(
        string ModuloClave,
        bool   PuedeLeer,
        bool   PuedeEscribir,
        bool   PuedeEliminar,
        bool   Denegado
    );
}
