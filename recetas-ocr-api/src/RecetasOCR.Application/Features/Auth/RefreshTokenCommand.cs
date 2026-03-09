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

        // 2. Look up session in BD (sin filtrar por estado para detectar reuso)
        var sesion = await db.Database
            .SqlQuery<SesionRow>($"""
                SELECT s.Id, s.IdUsuario, s.JwtTokenId, s.Estado, s.FechaExpiracion,
                       s.MotivoRevocacion, s.FechaUltimaActividad,
                       u.Username, u.NombreCompleto, u.Email,
                       r.Clave AS RolClave
                FROM   seg.Sesiones s
                INNER JOIN seg.Usuarios u ON u.Id = s.IdUsuario
                INNER JOIN seg.Roles    r ON r.Id = u.IdRol
                WHERE  s.RefreshToken = {request.RefreshToken}
                  AND  u.Activo       = 1
                """)
            .FirstOrDefaultAsync(ct);

        if (sesion is null)
        {
            logger.LogWarning("[Auth] REFRESH_FALLIDO — Token no encontrado en BD");
            throw new UnauthorizedAccessException("Sesión no encontrada. Por favor, inicie sesión nuevamente.");
        }

        // 3. Verificar estado de la sesión
        if (sesion.Estado != "ACTIVA")
        {
            // Detectar reuso de refresh token (posible ataque o retry)
            if (sesion.MotivoRevocacion == "RENOVADA")
            {
                // Grace period de 10 segundos para reintentos legítimos
                var segundosDesdeRevocacion = (DateTime.UtcNow - sesion.FechaUltimaActividad).TotalSeconds;
                if (segundosDesdeRevocacion <= 10)
                {
                    logger.LogWarning(
                        "[Auth] REFRESH_REINTENTO — {Username} reintentó con token ya usado ({Segundos:F1}s después)",
                        sesion.Username, segundosDesdeRevocacion);
                    throw new UnauthorizedAccessException(
                        "Este token ya fue renovado recientemente. Use el nuevo token recibido.");
                }

                // Fuera del grace period: posible ataque de reuso
                logger.LogError(
                    "[Auth] REFRESH_REUSO_DETECTADO — {Username} intentó reusar token renovado hace {Segundos:F0}s. Revocando todas las sesiones.",
                    sesion.Username, segundosDesdeRevocacion);

                // Revocar TODAS las sesiones del usuario por seguridad (refresh token rotation detection)
                await db.Database.ExecuteSqlAsync($"""
                    UPDATE seg.Sesiones
                    SET    Estado = 'CERRADA', MotivoRevocacion = 'REUSO_DETECTADO'
                    WHERE  IdUsuario = {sesion.IdUsuario}
                      AND  Estado = 'ACTIVA'
                    """, ct);
                await db.SaveChangesAsync(ct);

                throw new UnauthorizedAccessException(
                    "Posible reuso de token detectado. Todas las sesiones han sido revocadas por seguridad. Inicie sesión nuevamente.");
            }

            logger.LogWarning(
                "[Auth] REFRESH_SESION_INVALIDA — {Username} | Estado: {Estado} | Motivo: {Motivo}",
                sesion.Username, sesion.Estado, sesion.MotivoRevocacion ?? "N/A");
            throw new UnauthorizedAccessException(
                $"La sesión fue {sesion.Estado.ToLower()}. Por favor, inicie sesión nuevamente.");
        }

        // 4. Verificar expiración
        if (sesion.FechaExpiracion < DateTime.UtcNow)
        {
            await RevocarAsync(sesion.Id, "EXPIRADA", ct);
            await db.SaveChangesAsync(ct);
            logger.LogInformation(
                "[Auth] REFRESH_EXPIRADO — {Username} | Expiró: {FechaExpiracion}",
                sesion.Username, sesion.FechaExpiracion);
            throw new UnauthorizedAccessException("La sesión expiró. Por favor, inicie sesión nuevamente.");
        }

        // 5. Bloqueo optimista: actualizar timestamp antes de procesar
        // Esto previene condiciones de carrera si 2 requests llegan simultáneamente
        var rowsUpdated = await db.Database.ExecuteSqlAsync($"""
            UPDATE seg.Sesiones
            SET    FechaUltimaActividad = GETUTCDATE()
            WHERE  Id = {sesion.Id}
              AND  Estado = 'ACTIVA'
              AND  RefreshToken = {request.RefreshToken}
            """, ct);

        if (rowsUpdated == 0)
        {
            logger.LogWarning(
                "[Auth] REFRESH_RACE_CONDITION — {Username} | Sesión fue modificada por otro request",
                sesion.Username);
            throw new UnauthorizedAccessException(
                "La sesión fue modificada por otra solicitud. Intente nuevamente.");
        }

        // 6. Recalculate effective permissions
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

        // 7. Generate new tokens
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

        // 8. Revoke old session with timestamp, insert new one
        await db.Database.ExecuteSqlAsync($"""
            UPDATE seg.Sesiones
            SET    Estado = 'CERRADA', 
                   MotivoRevocacion = 'RENOVADA',
                   FechaUltimaActividad = GETUTCDATE()
            WHERE  Id = {sesion.Id}
            """, ct);

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
        Guid      Id,
        Guid      IdUsuario,
        string    JwtTokenId,
        string    Estado,
        DateTime  FechaExpiracion,
        string?   MotivoRevocacion,
        DateTime  FechaUltimaActividad,
        string    Username,
        string    NombreCompleto,
        string    Email,
        string    RolClave
    );

    private sealed record PermisoRow(
        string ModuloClave,
        bool   PuedeLeer,
        bool   PuedeEscribir,
        bool   PuedeEliminar,
        bool   Denegado
    );
}
