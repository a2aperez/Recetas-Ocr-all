[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RecetasOCR.Application.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace RecetasOCR.Application.Features.Auth;

// ── Internal row types (query results for ILoginQueryRepository) ──────────────
// Declared internal; test project can access them via InternalsVisibleTo above.

internal sealed record UsuarioLoginData(
    Guid      Id,
    string    Username,
    string    Email,
    string    PasswordHash,
    string    NombreCompleto,
    bool      Activo,
    int       IntentosFallidos,
    DateTime? BloqueadoHasta,
    DateTime? UltimoAcceso,
    DateTime  FechaActualizacion,
    int       IdRol,
    string    RolClave
);

internal sealed record PermisoLoginData(
    string ModuloClave,
    bool   PuedeLeer,
    bool   PuedeEscribir,
    bool   PuedeEliminar,
    bool   Denegado
);

/// <summary>
/// Abstracción de los accesos a datos de autenticación.
/// Permite mockear la capa de datos en pruebas unitarias de LoginCommandHandler.
/// Implementado por LoginQueryRepository en la misma capa Application.
/// </summary>
internal interface ILoginQueryRepository
{
    Task<UsuarioLoginData?> BuscarUsuarioAsync(string input, CancellationToken ct);

    Task<List<PermisoLoginData>> ObtenerPermisosRolAsync(int idRol, CancellationToken ct);

    Task<List<PermisoLoginData>> ObtenerPermisosUsuarioAsync(Guid idUsuario, CancellationToken ct);

    Task IncrementarIntentosFallidosAsync(
        Guid       id,
        int        nuevosIntentos,
        DateTime?  bloqueadoHasta,
        CancellationToken ct);

    Task ResetearContadoresAsync(Guid id, CancellationToken ct);

    Task InsertarSesionAsync(
        Guid      sesionId,
        Guid      idUsuario,
        string    jwtTokenId,
        string    refreshToken,
        DateTime  expiraEn,
        CancellationToken ct);

    Task InsertarLogAccesoAsync(Guid? idUsuario, string evento, string detalle, CancellationToken ct);

    Task<int> SaveAsync(CancellationToken ct);
}
