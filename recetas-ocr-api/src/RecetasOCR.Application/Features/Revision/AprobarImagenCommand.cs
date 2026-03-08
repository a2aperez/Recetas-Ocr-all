using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Common;
using RecetasOCR.Domain.Enums;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Revision;

// ──────────────────────────────────────────────────────────────────────────────
// Command
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Aprueba una imagen revisada por un humano.
/// Pre-condición: imagen debe estar en EstadosValidos.ImagenesPendientesRevision().
/// Post-efecto:   estado de imagen → REVISADA.
///                Si TODAS las imágenes del grupo están en estados finales
///                → estado de grupo → REVISADO_COMPLETO.
/// Implementa IAuditableCommand: requiere usuario autenticado.
/// </summary>
public record AprobarImagenCommand(
    Guid    IdImagen,
    string? Observaciones
) : IRequest<Unit>, IAuditableCommand;

// ──────────────────────────────────────────────────────────────────────────────
// Validator
// ──────────────────────────────────────────────────────────────────────────────

public class AprobarImagenCommandValidator : AbstractValidator<AprobarImagenCommand>
{
    public AprobarImagenCommandValidator()
    {
        RuleFor(x => x.IdImagen)
            .NotEmpty()
            .WithMessage("El IdImagen es obligatorio.");

        RuleFor(x => x.Observaciones)
            .MaximumLength(500)
            .WithMessage("Las observaciones no pueden exceder 500 caracteres.")
            .When(x => x.Observaciones is not null);
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Handler
// ──────────────────────────────────────────────────────────────────────────────

public class AprobarImagenCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser,
    ILogger<AprobarImagenCommandHandler> logger)
    : IRequestHandler<AprobarImagenCommand, Unit>
{
    // Claves de cat.EstadosImagen que representan estados finales
    private static readonly string[] _estadosFinalesImagen =
        EstadosValidos.ImagenesFinales()
            .Select(e => e.ToString().ToUpperInvariant())
            .ToArray();

    // Claves de cat.EstadosImagen válidos para iniciar revisión
    private static readonly string[] _estadosPendientesRevision =
        EstadosValidos.ImagenesPendientesRevision()
            .Select(e => e.ToString().ToUpperInvariant())
            .ToArray();

    public async Task<Unit> Handle(
        AprobarImagenCommand command,
        CancellationToken    cancellationToken)
    {
        var ahora    = DateTime.UtcNow;
        var usuarioId = currentUser.UserId!.Value;
        var username  = currentUser.Username;

        // ── 1. Cargar imagen + estado actual ───────────────────────────
        var imagen = await db.Database
            .SqlQuery<ImagenEstadoRow>($"""
                SELECT i.Id, i.IdGrupo, i.IdEstadoImagen,
                       ei.Clave AS EstadoClave
                FROM   rec.Imagenes       i
                INNER JOIN cat.EstadosImagen ei ON ei.Id = i.IdEstadoImagen
                WHERE  i.Id = {command.IdImagen}
                """)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new EntidadNoEncontradaException("Imagen", command.IdImagen);

        // ── 2. Verificar estado en pendientes de revisión ──────────────
        // Comparamos en mayúsculas contra las claves del enum
        if (!_estadosPendientesRevision.Contains(
                imagen.EstadoClave.ToUpperInvariant(),
                StringComparer.OrdinalIgnoreCase))
        {
            throw new EstadoInvalidoException(
                "Imagen",
                imagen.EstadoClave,
                _estadosPendientesRevision);
        }

        // ── 3. Obtener Id del estado REVISADA ──────────────────────────
        var estadoRevisada = await db.Database
            .SqlQuery<EstadoRow>($"""
                SELECT Id, Clave FROM cat.EstadosImagen WHERE Clave = 'REVISADA'
                """)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new EntidadNoEncontradaException("cat.EstadosImagen", "REVISADA");

        // ── 4. UPDATE rec.Imagenes → REVISADA ─────────────────────────
        await db.Database.ExecuteSqlAsync($"""
            UPDATE rec.Imagenes
            SET    IdEstadoImagen   = {estadoRevisada.Id},
                   FechaActualizacion = {ahora},
                   ModificadoPor    = {username},
                   FechaModificacion = {ahora}
            WHERE  Id = {command.IdImagen}
            """, cancellationToken);

        // ── 5. INSERT aud.HistorialEstadosImagen ───────────────────────
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO aud.HistorialEstadosImagen
                (IdImagen, EstadoAnterior, EstadoNuevo, IdUsuario, Motivo, FechaCambio)
            VALUES
                ({command.IdImagen}, {imagen.IdEstadoImagen}, {estadoRevisada.Id},
                 {usuarioId}, {command.Observaciones ?? "Imagen aprobada"}, {ahora})
            """, cancellationToken);

        // ── 6. INSERT rev.RevisionesHumanas Resultado=APROBADA ─────────
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO rev.RevisionesHumanas
                (Id, IdImagen, IdGrupo, TipoRevision, Resultado,
                 IdUsuarioRevisor, FechaRevision, Observaciones,
                 ModificadoPor, FechaModificacion)
            VALUES
                ({Guid.NewGuid()}, {command.IdImagen}, {imagen.IdGrupo},
                 'VALIDACION', 'APROBADA',
                 {usuarioId}, {ahora}, {command.Observaciones},
                 {username}, {ahora})
            """, cancellationToken);

        // ── 7. Verificar si TODAS las imágenes del grupo están en final ─
        var pendientes = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value
                FROM   rec.Imagenes       i
                INNER JOIN cat.EstadosImagen ei ON ei.Id = i.IdEstadoImagen
                WHERE  i.IdGrupo = {imagen.IdGrupo}
                  AND  ei.EsFinal = 0
                  AND  i.Id <> {command.IdImagen}   -- excluir la que acabamos de cambiar
                """)
            .FirstAsync(cancellationToken);

        if (pendientes == 0)
        {
            // ── 8a. Obtener estado actual del grupo ────────────────────
            var grupo = await db.Database
                .SqlQuery<GrupoEstadoRow>($"""
                    SELECT g.Id, g.IdEstadoGrupo, eg.Clave AS EstadoClave
                    FROM   rec.GruposReceta  g
                    INNER JOIN cat.EstadosGrupo eg ON eg.Id = g.IdEstadoGrupo
                    WHERE  g.Id = {imagen.IdGrupo}
                    """)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new EntidadNoEncontradaException("GrupoReceta", imagen.IdGrupo);

            var estadoRevisadoCompleto = await db.Database
                .SqlQuery<EstadoRow>($"""
                    SELECT Id, Clave FROM cat.EstadosGrupo WHERE Clave = 'REVISADO_COMPLETO'
                    """)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new EntidadNoEncontradaException("cat.EstadosGrupo", "REVISADO_COMPLETO");

            // ── 8b. UPDATE rec.GruposReceta → REVISADO_COMPLETO ───────
            await db.Database.ExecuteSqlAsync($"""
                UPDATE rec.GruposReceta
                SET    IdEstadoGrupo      = {estadoRevisadoCompleto.Id},
                       FechaActualizacion = {ahora},
                       ModificadoPor      = {username},
                       FechaModificacion  = {ahora}
                WHERE  Id = {imagen.IdGrupo}
                """, cancellationToken);

            // ── 8c. INSERT aud.HistorialEstadosGrupo ──────────────────
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO aud.HistorialEstadosGrupo
                    (IdGrupo, EstadoAnterior, EstadoNuevo, IdUsuario, Motivo, FechaCambio)
                VALUES
                    ({imagen.IdGrupo}, {grupo.IdEstadoGrupo}, {estadoRevisadoCompleto.Id},
                     {usuarioId}, 'Todas las imágenes revisadas', {ahora})
                """, cancellationToken);

            logger.LogInformation(
                "[Revision] Grupo {IdGrupo} → REVISADO_COMPLETO", imagen.IdGrupo);
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "[Revision] Imagen {IdImagen} APROBADA por {Usuario}", command.IdImagen, username);

        return Unit.Value;
    }

    // ── Tipos locales ──────────────────────────────────────────────────
    internal sealed record ImagenEstadoRow(Guid Id, Guid IdGrupo, int IdEstadoImagen, string EstadoClave);
    internal sealed record GrupoEstadoRow(Guid Id, int IdEstadoGrupo, string EstadoClave);
    internal sealed record EstadoRow(int Id, string Clave);
}
