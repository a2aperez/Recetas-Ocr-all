using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Common;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Revision;

// ──────────────────────────────────────────────────────────────────────────────
// Command
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Rechaza una imagen en revisión humana.
/// Pre-condición: imagen debe estar en EstadosValidos.ImagenesPendientesRevision().
/// Post-efecto:   estado de imagen → RECHAZADA.
///                Si TODAS las imágenes del grupo están en estados finales
///                → estado de grupo → REVISADO_COMPLETO.
/// MotivoRechazo es obligatorio — queda registrado en RevisionesHumanas
/// y en aud.HistorialEstadosImagen.
/// </summary>
public record RechazarImagenCommand(
    Guid   IdImagen,
    string MotivoRechazo,
    string? Observaciones = null
) : IRequest<Unit>, IAuditableCommand;

// ──────────────────────────────────────────────────────────────────────────────
// Validator
// ──────────────────────────────────────────────────────────────────────────────

public class RechazarImagenCommandValidator : AbstractValidator<RechazarImagenCommand>
{
    public RechazarImagenCommandValidator()
    {
        RuleFor(x => x.IdImagen)
            .NotEmpty()
            .WithMessage("El IdImagen es obligatorio.");

        RuleFor(x => x.MotivoRechazo)
            .NotEmpty()
            .WithMessage("El motivo de rechazo es obligatorio.")
            .MaximumLength(300)
            .WithMessage("El motivo de rechazo no puede exceder 300 caracteres.");

        RuleFor(x => x.Observaciones)
            .MaximumLength(500)
            .WithMessage("Las observaciones no pueden exceder 500 caracteres.")
            .When(x => x.Observaciones is not null);
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Handler
// ──────────────────────────────────────────────────────────────────────────────

public class RechazarImagenCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser,
    ILogger<RechazarImagenCommandHandler> logger)
    : IRequestHandler<RechazarImagenCommand, Unit>
{
    private static readonly string[] _estadosPendientesRevision =
        EstadosValidos.ImagenesPendientesRevision()
            .Select(e => e.ToString().ToUpperInvariant())
            .ToArray();

    public async Task<Unit> Handle(
        RechazarImagenCommand command,
        CancellationToken     cancellationToken)
    {
        var ahora     = DateTime.UtcNow;
        var usuarioId = currentUser.UserId!.Value;
        var username  = currentUser.Username;

        // ── 1. Cargar imagen + estado actual ───────────────────────────
        var imagen = await db.Database
            .SqlQuery<AprobarImagenCommandHandler.ImagenEstadoRow>($"""
                SELECT i.Id, i.IdGrupo, i.IdEstadoImagen,
                       ei.Clave AS EstadoClave
                FROM   rec.Imagenes       i
                INNER JOIN cat.EstadosImagen ei ON ei.Id = i.IdEstadoImagen
                WHERE  i.Id = {command.IdImagen}
                """)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new EntidadNoEncontradaException("Imagen", command.IdImagen);

        // ── 2. Verificar estado en pendientes de revisión ──────────────
        if (!_estadosPendientesRevision.Contains(
                imagen.EstadoClave.ToUpperInvariant(),
                StringComparer.OrdinalIgnoreCase))
        {
            throw new EstadoInvalidoException(
                "Imagen",
                imagen.EstadoClave,
                _estadosPendientesRevision);
        }

        // ── 3. Obtener Id del estado RECHAZADA ─────────────────────────
        var estadoRechazada = await db.Database
            .SqlQuery<AprobarImagenCommandHandler.EstadoRow>($"""
                SELECT Id, Clave FROM cat.EstadosImagen WHERE Clave = 'RECHAZADA'
                """)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new EntidadNoEncontradaException("cat.EstadosImagen", "RECHAZADA");

        // ── 4. UPDATE rec.Imagenes → RECHAZADA ────────────────────────
        await db.Database.ExecuteSqlAsync($"""
            UPDATE rec.Imagenes
            SET    IdEstadoImagen    = {estadoRechazada.Id},
                   FechaActualizacion = {ahora},
                   ModificadoPor     = {username},
                   FechaModificacion  = {ahora}
            WHERE  Id = {command.IdImagen}
            """, cancellationToken);

        // ── 5. INSERT aud.HistorialEstadosImagen ───────────────────────
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO aud.HistorialEstadosImagen
                (IdImagen, EstadoAnterior, EstadoNuevo, IdUsuario, Motivo, FechaCambio)
            VALUES
                ({command.IdImagen}, {imagen.IdEstadoImagen}, {estadoRechazada.Id},
                 {usuarioId}, {command.MotivoRechazo}, {ahora})
            """, cancellationToken);

        // ── 6. INSERT rev.RevisionesHumanas Resultado=RECHAZADA ────────
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO rev.RevisionesHumanas
                (Id, IdImagen, IdGrupo, TipoRevision, Resultado, MotivoRechazo,
                 IdUsuarioRevisor, FechaRevision, Observaciones,
                 ModificadoPor, FechaModificacion)
            VALUES
                ({Guid.NewGuid()}, {command.IdImagen}, {imagen.IdGrupo},
                 'VALIDACION', 'RECHAZADA', {command.MotivoRechazo},
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
                  AND  i.Id <> {command.IdImagen}
                """)
            .FirstAsync(cancellationToken);

        if (pendientes == 0)
        {
            var grupo = await db.Database
                .SqlQuery<AprobarImagenCommandHandler.GrupoEstadoRow>($"""
                    SELECT g.Id, g.IdEstadoGrupo, eg.Clave AS EstadoClave
                    FROM   rec.GruposReceta  g
                    INNER JOIN cat.EstadosGrupo eg ON eg.Id = g.IdEstadoGrupo
                    WHERE  g.Id = {imagen.IdGrupo}
                    """)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new EntidadNoEncontradaException("GrupoReceta", imagen.IdGrupo);

            var estadoRevisadoCompleto = await db.Database
                .SqlQuery<AprobarImagenCommandHandler.EstadoRow>($"""
                    SELECT Id, Clave FROM cat.EstadosGrupo WHERE Clave = 'REVISADO_COMPLETO'
                    """)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new EntidadNoEncontradaException("cat.EstadosGrupo", "REVISADO_COMPLETO");

            await db.Database.ExecuteSqlAsync($"""
                UPDATE rec.GruposReceta
                SET    IdEstadoGrupo      = {estadoRevisadoCompleto.Id},
                       FechaActualizacion = {ahora},
                       ModificadoPor      = {username},
                       FechaModificacion  = {ahora}
                WHERE  Id = {imagen.IdGrupo}
                """, cancellationToken);

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
            "[Revision] Imagen {IdImagen} RECHAZADA por {Usuario}. Motivo: {Motivo}",
            command.IdImagen, username, command.MotivoRechazo);

        return Unit.Value;
    }
}
