using System.Security.Claims;
using Bogus;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Auth;
using RecetasOCR.Application.Features.Auth;
using RecetasOCR.Domain.Common;

namespace RecetasOCR.Application.Tests.Features.Auth;

public class LoginCommandHandlerTests
{
    // ── Mocks ──────────────────────────────────────────────────────────────────
    private readonly Mock<ILoginQueryRepository> _repo        = new();
    private readonly Mock<IJwtService>           _jwtService  = new();
    private readonly Mock<IParametrosService>    _parametros  = new();
    private readonly Mock<IPasswordHasherService>_hasher      = new();

    private readonly LoginCommandHandler _handler;

    private static readonly Faker Fake = new("es_MX");

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _repo.Object,
            _jwtService.Object,
            _parametros.Object,
            _hasher.Object,
            NullLogger<LoginCommandHandler>.Instance);

        // Default parametros: return defaults for every call
        _parametros
            .Setup(p => p.ObtenerIntAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, int def, CancellationToken _) => def);

        // Default repo write operations: succeed silently
        _repo.Setup(r => r.InsertarLogAccesoAsync(
                It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(1);
        _repo.Setup(r => r.IncrementarIntentosFallidosAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        _repo.Setup(r => r.ResetearContadoresAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        _repo.Setup(r => r.InsertarSesionAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        _repo.Setup(r => r.ObtenerPermisosRolAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);
        _repo.Setup(r => r.ObtenerPermisosUsuarioAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static UsuarioLoginData BuildUsuario(
        int       intentosFallidos = 0,
        DateTime? bloqueadoHasta   = null)
    {
        return new UsuarioLoginData(
            Id:                 Guid.NewGuid(),
            Username:           Fake.Internet.UserName(),
            Email:              Fake.Internet.Email(),
            PasswordHash:       "$2a$12$fakehashedpassword",
            NombreCompleto:     Fake.Name.FullName(),
            Activo:             true,
            IntentosFallidos:   intentosFallidos,
            BloqueadoHasta:     bloqueadoHasta,
            UltimoAcceso:       DateTime.UtcNow.AddDays(-1),
            FechaActualizacion: DateTime.UtcNow.AddDays(-1),
            IdRol:              1,
            RolClave:           "OPERADOR"
        );
    }

    private void SetupJwtSuccess(string token = "header.payload.signature")
    {
        var claims = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("jti", Guid.NewGuid().ToString())]));

        _jwtService.Setup(j => j.GenerarToken(It.IsAny<UsuarioDto>())).Returns(token);
        _jwtService.Setup(j => j.GenerarRefreshToken()).Returns("refresh-abc123");
        _jwtService.Setup(j => j.ObtenerClaimsDeToken(token)).Returns(claims);
    }

    // ── Caso 1: Login exitoso ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CredencialesValidas_RetornaLoginResponseDtoConToken()
    {
        // Arrange
        var usuario = BuildUsuario();
        const string expectedToken = "valid.jwt.token";

        _repo.Setup(r => r.BuscarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(usuario);
        _hasher.Setup(h => h.Verificar(It.IsAny<string>(), usuario.PasswordHash)).Returns(true);
        SetupJwtSuccess(expectedToken);

        var command = new LoginCommand(usuario.Username, "P@ssw0rd!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be(expectedToken);
        result.RefreshToken.Should().Be("refresh-abc123");
        result.ExpiraEn.Should().BeAfter(DateTime.UtcNow);
        result.Usuario.Username.Should().Be(usuario.Username);
        result.Usuario.Rol.Should().Be(usuario.RolClave);

        _repo.Verify(r => r.ResetearContadoresAsync(usuario.Id, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.InsertarSesionAsync(
            It.IsAny<Guid>(), usuario.Id, It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Caso 2: Usuario no existe ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_UsuarioNoExiste_LanzaUnauthorizedSinRevelarSiExiste()
    {
        // Arrange
        _repo.Setup(r => r.BuscarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((UsuarioLoginData?)null);

        var command = new LoginCommand("usuario.inexistente", "cualquier");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert — mensaje genérico, nunca revela si el usuario existe
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Credenciales incorrectas.");

        _repo.Verify(r => r.InsertarLogAccesoAsync(
            null, "LOGIN_FALLIDO", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _hasher.Verify(h => h.Verificar(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── Caso 3: Contraseña incorrecta → IntentosFallidos++ ────────────────────

    [Fact]
    public async Task Handle_PasswordIncorrecto_IncrementaIntentosFallidosSinBloquear()
    {
        // Arrange
        var usuario = BuildUsuario(intentosFallidos: 0);
        _repo.Setup(r => r.BuscarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(usuario);
        _hasher.Setup(h => h.Verificar(It.IsAny<string>(), usuario.PasswordHash)).Returns(false);
        // MAX_INTENTOS_LOGIN = 5 (default returned by parametros setup)

        var command = new LoginCommand(usuario.Username, "wrong-password");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Credenciales incorrectas.");

        // Intentos: 0 → 1, sin bloqueo (1 < 5)
        _repo.Verify(r => r.IncrementarIntentosFallidosAsync(
            usuario.Id,
            1,
            null,               // no bloqueada aún
            It.IsAny<CancellationToken>()), Times.Once);

        _repo.Verify(r => r.InsertarLogAccesoAsync(
            usuario.Id, "LOGIN_FALLIDO", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Caso 4: Cuenta bloqueada → error con tiempo restante ──────────────────

    [Fact]
    public async Task Handle_CuentaBloqueada_LanzaExcepcionConTiempoRestante()
    {
        // Arrange — BloqueadoHasta en el futuro
        var bloqueadoHasta = DateTime.UtcNow.AddMinutes(25);
        var usuario = BuildUsuario(intentosFallidos: 5, bloqueadoHasta: bloqueadoHasta);
        _repo.Setup(r => r.BuscarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(usuario);

        var command = new LoginCommand(usuario.Username, "cualquier");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*bloqueada*");

        // Nunca debe verificar la contraseña mientras la cuenta esté bloqueada
        _hasher.Verify(h => h.Verificar(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        _repo.Verify(r => r.InsertarLogAccesoAsync(
            usuario.Id, "LOGIN_BLOQUEADO", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Caso 5: IntentosFallidos >= MAX → bloquea cuenta ──────────────────────

    [Fact]
    public async Task Handle_AlcanzaMaxIntentos_BloquearCuentaConFechaFutura()
    {
        // Arrange — 4 intentos previos, max = 5
        var usuario = BuildUsuario(intentosFallidos: 4);
        _repo.Setup(r => r.BuscarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(usuario);
        _hasher.Setup(h => h.Verificar(It.IsAny<string>(), usuario.PasswordHash)).Returns(false);

        // Configurar explícitamente los parámetros relevantes
        _parametros.Setup(p => p.ObtenerIntAsync(
                Constantes.Parametros.MAX_INTENTOS_LOGIN, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _parametros.Setup(p => p.ObtenerIntAsync(
                Constantes.Parametros.BLOQUEO_MINUTOS, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(30);

        var command = new LoginCommand(usuario.Username, "wrong");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Credenciales incorrectas.");

        // El 5.° intento (4+1=5) debe disparar el bloqueo con fecha no nula
        _repo.Verify(r => r.IncrementarIntentosFallidosAsync(
            usuario.Id,
            5,
            It.Is<DateTime?>(d => d.HasValue && d.Value > DateTime.UtcNow),
            It.IsAny<CancellationToken>()), Times.Once);

        _repo.Verify(r => r.InsertarLogAccesoAsync(
            usuario.Id, "CUENTA_BLOQUEADA", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Caso 6: Login después de bloqueo expirado → resetea IntentosFallidos ──

    [Fact]
    public async Task Handle_BloqueoExpirado_PermiteLoginYResetearIntentosFallidos()
    {
        // Arrange — BloqueadoHasta en el pasado (bloqueo expiró)
        var bloqueadoHasta = DateTime.UtcNow.AddHours(-1);
        var usuario = BuildUsuario(intentosFallidos: 5, bloqueadoHasta: bloqueadoHasta);

        _repo.Setup(r => r.BuscarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(usuario);
        _hasher.Setup(h => h.Verificar(It.IsAny<string>(), usuario.PasswordHash)).Returns(true);
        SetupJwtSuccess();

        var command = new LoginCommand(usuario.Username, "correct-password");

        // Act — no debe lanzar excepción
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — login exitoso y contadores reseteados
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();

        _repo.Verify(r => r.ResetearContadoresAsync(usuario.Id, It.IsAny<CancellationToken>()), Times.Once);

        // No debe haber llamado a IncrementarIntentosFallidos
        _repo.Verify(r => r.IncrementarIntentosFallidosAsync(
            It.IsAny<Guid>(), It.IsAny<int>(),
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
