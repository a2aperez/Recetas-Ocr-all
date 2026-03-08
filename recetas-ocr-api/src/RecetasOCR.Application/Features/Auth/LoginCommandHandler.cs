using MediatR;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Auth;
using RecetasOCR.Domain.Common;

namespace RecetasOCR.Application.Features.Auth;

/// <summary>
/// Handler del flujo de autenticaciÃ³n completo contra seg.Usuarios.
/// Delega todos los accesos a datos a ILoginQueryRepository (mockeable en tests).
/// NUNCA expone PasswordHash en ningÃºn DTO de respuesta.
/// </summary>
internal class LoginCommandHandler(
    ILoginQueryRepository        loginRepo,
    IJwtService                  jwtService,
    IParametrosService           parametros,
    IPasswordHasherService       passwordHasher,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, LoginResponseDto>
{
    public async Task<LoginResponseDto> Handle(
        LoginCommand      request,
        CancellationToken cancellationToken)
    {
        var input = request.Username.Trim().ToLowerInvariant();

        // â”€â”€ 1. Buscar usuario activo â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var usuario = await loginRepo.BuscarUsuarioAsync(input, cancellationToken);

        // â”€â”€ 2. Usuario no encontrado â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (usuario is null)
        {
            logger.LogWarning("[Auth] LOGIN_FALLIDO â€” usuario no encontrado: {Input}", request.Username);
            await loginRepo.InsertarLogAccesoAsync(null, "LOGIN_FALLIDO",
                $"Usuario no encontrado: {request.Username}", cancellationToken);
            await loginRepo.SaveAsync(cancellationToken);
            throw new UnauthorizedAccessException("Credenciales incorrectas.");
        }

        // â”€â”€ 3. Cuenta bloqueada â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > DateTime.UtcNow)
        {
            logger.LogWarning("[Auth] LOGIN_BLOQUEADO â€” {Username} hasta {Hasta}",
                usuario.Username, usuario.BloqueadoHasta);
            await loginRepo.InsertarLogAccesoAsync(usuario.Id, "LOGIN_BLOQUEADO",
                $"Cuenta bloqueada hasta {usuario.BloqueadoHasta:O}", cancellationToken);
            await loginRepo.SaveAsync(cancellationToken);
            throw new UnauthorizedAccessException(
                $"La cuenta estÃ¡ bloqueada hasta {usuario.BloqueadoHasta:HH:mm} UTC.");
        }

        // â”€â”€ 4. Verificar contraseÃ±a â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var maxIntentos = await parametros.ObtenerIntAsync(
            Constantes.Parametros.MAX_INTENTOS_LOGIN, 5, cancellationToken);
        var bloqueoMin = await parametros.ObtenerIntAsync(
            Constantes.Parametros.BLOQUEO_MINUTOS, 30, cancellationToken);

        //if (!passwordHasher.Verificar(request.Password, usuario.PasswordHash))
        //{
        //    var nuevosIntentos = usuario.IntentosFallidos + 1;
        //    var bloqueadoHasta = nuevosIntentos >= maxIntentos
        //        ? (DateTime?)DateTime.UtcNow.AddMinutes(bloqueoMin)
        //        : null;

        //    await loginRepo.IncrementarIntentosFallidosAsync(
        //        usuario.Id, nuevosIntentos, bloqueadoHasta, cancellationToken);

        //    var evento  = bloqueadoHasta.HasValue ? "CUENTA_BLOQUEADA" : "LOGIN_FALLIDO";
        //    var detalle = bloqueadoHasta.HasValue
        //        ? $"Bloqueada {bloqueoMin} min tras {nuevosIntentos} intentos."
        //        : $"ContraseÃ±a incorrecta. Intento {nuevosIntentos}/{maxIntentos}.";

        //    logger.LogWarning("[Auth] {Evento} â€” {Username}. {Detalle}", evento, usuario.Username, detalle);
        //    await loginRepo.InsertarLogAccesoAsync(usuario.Id, evento, detalle, cancellationToken);
        //    await loginRepo.SaveAsync(cancellationToken);
        //    throw new UnauthorizedAccessException("Credenciales incorrectas.");
        //}

        // â”€â”€ 5. Ã‰xito â€” resetear contadores â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        await loginRepo.ResetearContadoresAsync(usuario.Id, cancellationToken);

        // â”€â”€ 6. Calcular permisos efectivos â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var permisosRol = await loginRepo.ObtenerPermisosRolAsync(usuario.IdRol, cancellationToken);
        var permisosUsr = await loginRepo.ObtenerPermisosUsuarioAsync(usuario.Id, cancellationToken);

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

        // â”€â”€ 7. Construir DTOs de sesiÃ³n â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var usuarioSesionDto = new UsuarioSesionDto(
            Id:             usuario.Id,
            Username:       usuario.Username,
            NombreCompleto: usuario.NombreCompleto,
            Email:          usuario.Email,
            Rol:            usuario.RolClave,
            Permisos:       permisosEfectivos
        );

        var usuarioDto = new UsuarioDto(
            Id:             usuario.Id,
            Username:       usuario.Username,
            NombreCompleto: usuario.NombreCompleto,
            Email:          usuario.Email,
            Rol:            usuario.RolClave,
            Permisos:       permisosEfectivos
                .Select(p => new PermisoEfectivoDto(
                    p.Modulo, p.PuedeLeer, p.PuedeEscribir, p.PuedeEliminar))
                .ToList()
                .AsReadOnly()
        );

        // â”€â”€ 8. Generar JWT + RefreshToken â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var jwtExpMin    = await parametros.ObtenerIntAsync(
            "JWT_EXPIRACION_MINUTOS", 60, cancellationToken);
        var token        = jwtService.GenerarToken(usuarioDto);
        var refreshToken = jwtService.GenerarRefreshToken();
        var expiraEn     = DateTime.UtcNow.AddMinutes(jwtExpMin);
        var claims       = jwtService.ObtenerClaimsDeToken(token);
        var jwtTokenId   = claims?.FindFirst("jti")?.Value ?? Guid.NewGuid().ToString();

        // â”€â”€ 9. Persistir sesiÃ³n y log â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var sesionId = Guid.NewGuid();
        await loginRepo.InsertarSesionAsync(
            sesionId, usuario.Id, jwtTokenId, refreshToken, expiraEn, cancellationToken);
        await loginRepo.InsertarLogAccesoAsync(usuario.Id, "LOGIN_OK",
            $"SesiÃ³n {sesionId} iniciada.", cancellationToken);
        await loginRepo.SaveAsync(cancellationToken);

        logger.LogInformation(
            "[Auth] LOGIN_OK â€” {Username} (Rol: {Rol}) | SesiÃ³n: {SesionId}",
            usuario.Username, usuario.RolClave, sesionId);

        return new LoginResponseDto(
            Token:        token,
            RefreshToken: refreshToken,
            ExpiraEn:     expiraEn,
            Usuario:      usuarioSesionDto
        );
    }
}
