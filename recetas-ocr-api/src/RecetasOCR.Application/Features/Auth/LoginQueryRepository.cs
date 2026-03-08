using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;

namespace RecetasOCR.Application.Features.Auth;

/// <summary>
/// Implementación de ILoginQueryRepository que ejecuta el SQL contra
/// IRecetasOcrDbContext. Vive en Application para no requerir referencia a Infrastructure.
/// </summary>
internal class LoginQueryRepository(IRecetasOcrDbContext db) : ILoginQueryRepository
{
    public Task<UsuarioLoginData?> BuscarUsuarioAsync(string input, CancellationToken ct) =>
        db.Database
            .SqlQuery<UsuarioLoginData>($"""
                SELECT
                    u.Id, u.Username, u.Email, u.PasswordHash,
                    u.NombreCompleto, u.Activo,
                    u.IntentosFallidos, u.BloqueadoHasta, u.UltimoAcceso,
                    u.FechaActualizacion,
                    u.IdRol,
                    r.Clave AS RolClave
                FROM seg.Usuarios u
                INNER JOIN seg.Roles r ON r.Id = u.IdRol
                WHERE u.Activo = 1
                  AND (LOWER(u.Username) = {input} OR LOWER(u.Email) = {input})
                """)
            .FirstOrDefaultAsync(ct);

    public Task<List<PermisoLoginData>> ObtenerPermisosRolAsync(int idRol, CancellationToken ct) =>
        db.Database
            .SqlQuery<PermisoLoginData>($"""
                SELECT m.Clave AS ModuloClave,
                       pr.PuedeLeer, pr.PuedeEscribir, pr.PuedeEliminar,
                       CAST(0 AS BIT) AS Denegado
                FROM seg.PermisosRol pr
                INNER JOIN seg.Modulos m ON m.Id = pr.IdModulo
                WHERE pr.IdRol = {idRol}
                  AND m.Activo = 1
                """)
            .ToListAsync(ct);

    public Task<List<PermisoLoginData>> ObtenerPermisosUsuarioAsync(Guid idUsuario, CancellationToken ct) =>
        db.Database
            .SqlQuery<PermisoLoginData>($"""
                SELECT m.Clave AS ModuloClave,
                       pu.PuedeLeer, pu.PuedeEscribir, pu.PuedeEliminar,
                       pu.Denegado
                FROM seg.PermisosUsuario pu
                INNER JOIN seg.Modulos m ON m.Id = pu.IdModulo
                WHERE pu.IdUsuario = {idUsuario}
                  AND m.Activo     = 1
                """)
            .ToListAsync(ct);

    public Task IncrementarIntentosFallidosAsync(
        Guid id, int nuevosIntentos, DateTime? bloqueadoHasta, CancellationToken ct) =>
        db.Database.ExecuteSqlAsync($"""
            UPDATE seg.Usuarios
            SET    IntentosFallidos   = {nuevosIntentos},
                   BloqueadoHasta     = {bloqueadoHasta},
                   FechaActualizacion = GETUTCDATE()
            WHERE  Id = {id}
            """, ct);

    public Task ResetearContadoresAsync(Guid id, CancellationToken ct) =>
        db.Database.ExecuteSqlAsync($"""
            UPDATE seg.Usuarios
            SET    IntentosFallidos   = 0,
                   BloqueadoHasta     = NULL,
                   UltimoAcceso       = GETUTCDATE(),
                   FechaActualizacion = GETUTCDATE()
            WHERE  Id = {id}
            """, ct);

    public Task InsertarSesionAsync(
        Guid sesionId, Guid idUsuario, string jwtTokenId,
        string refreshToken, DateTime expiraEn, CancellationToken ct) =>
        db.Database.ExecuteSqlAsync($"""
            INSERT INTO seg.Sesiones
                (Id, IdUsuario, JwtTokenId, RefreshToken,
                 FechaInicio, FechaExpiracion, FechaUltimaActividad, Estado)
            VALUES
                ({sesionId}, {idUsuario}, {jwtTokenId}, {refreshToken},
                 GETUTCDATE(), {expiraEn}, GETUTCDATE(), 'ACTIVA')
            """, ct);

    public Task InsertarLogAccesoAsync(Guid? idUsuario, string evento, string detalle, CancellationToken ct) =>
        db.Database.ExecuteSqlAsync($"""
            INSERT INTO seg.LogAcceso (IdUsuario, Evento, Detalle, FechaEvento)
            VALUES ({idUsuario}, {evento}, {detalle}, GETUTCDATE())
            """, ct);

    public Task<int> SaveAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}
