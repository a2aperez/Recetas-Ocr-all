using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Ocr;
using RecetasOCR.Domain.Enums;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Ocr;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Encola una imagen para ser reprocesada con alta prioridad.
/// Solo aplica si la imagen está en ILEGIBLE o OCR_BAJA_CONFIANZA.
/// </summary>
public record ReprocesarImagenCommand(
    Guid   IdImagen,
    string Motivo
) : IRequest<EstadoOcrDto>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class ReprocesarImagenCommandValidator : AbstractValidator<ReprocesarImagenCommand>
{
    public ReprocesarImagenCommandValidator()
    {
        RuleFor(x => x.IdImagen).NotEmpty();
        RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class ReprocesarImagenCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser,
    IMediator            mediator)
    : IRequestHandler<ReprocesarImagenCommand, EstadoOcrDto>
{
    // Estados de imagen desde los cuales se permite reprocesar
    private static readonly string[] _estadosPermitidos =
    [
        EstadoImagen.Ilegible.ToString().ToUpperInvariant(),
        EstadoImagen.OcrBajaConfianza.ToString().ToUpperInvariant()
    ];

    public async Task<EstadoOcrDto> Handle(
        ReprocesarImagenCommand command,
        CancellationToken       ct)
    {
        // ── 1. Obtener estado actual ──────────────────────────────────────────
        var imagen = await db.Database
            .SqlQuery<ImagenRow>($"""
                SELECT i.Id, i.UrlBlobRaw,
                       e.Clave AS EstadoImagen
                FROM   rec.Imagenes       i
                INNER  JOIN cat.EstadosImagen e ON e.Id = i.IdEstadoImagen
                WHERE  i.Id = {command.IdImagen}
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntidadNoEncontradaException("Imagen", command.IdImagen);

        // ── 2. Validar estado ─────────────────────────────────────────────────
        if (!_estadosPermitidos.Contains(imagen.EstadoImagen, StringComparer.OrdinalIgnoreCase))
            throw new EstadoInvalidoException("Imagen", imagen.EstadoImagen, _estadosPermitidos);

        // ── 3. Evitar duplicados en cola ──────────────────────────────────────
        var yaPendiente = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value
                FROM   ocr.ColaProcesamiento
                WHERE  IdImagen   = {command.IdImagen}
                  AND  EstadoCola IN ('PENDIENTE', 'PROCESANDO')
                """)
            .FirstAsync(ct);

        if (yaPendiente > 0)
            throw new InvalidOperationException(
                "La imagen ya tiene un proceso pendiente o en curso en la cola OCR.");

        var modificadoPor = currentUser.Username ?? "sistema";

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // ── 4. Insertar en cola con PRIORIDAD ALTA ────────────────────────
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO ocr.ColaProcesamiento
                    (IdImagen, UrlBlobRaw, Prioridad, Intentos, MaxIntentos,
                     FechaEncolado, Bloqueado, EstadoCola,
                     ModificadoPor, FechaModificacion)
                VALUES
                    ({command.IdImagen}, {imagen.UrlBlobRaw}, 1, 0, 3,
                     GETUTCDATE(), 0, 'PENDIENTE',
                     {modificadoPor}, GETUTCDATE())
                """, ct);

            // ── 5. Actualizar imagen a RECIBIDA ───────────────────────────────
            await db.Database.ExecuteSqlAsync($"""
                UPDATE rec.Imagenes
                SET    IdEstadoImagen    = (SELECT Id FROM cat.EstadosImagen WHERE Clave = 'RECIBIDA'),
                       ModificadoPor     = {modificadoPor},
                       FechaActualizacion = GETUTCDATE(),
                       FechaModificacion  = GETUTCDATE()
                WHERE  Id = {command.IdImagen}
                """, ct);

            // ── 6. Auditoría ──────────────────────────────────────────────────
            var mensajeLog = $"Reproceso manual: {command.Motivo}";
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO aud.LogProcesamiento
                    (IdImagen, Paso, Nivel, Mensaje, FechaEvento)
                VALUES
                    ({command.IdImagen}, 'COLA', 'INFO', {mensajeLog}, GETUTCDATE())
                """, ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        // ── 7. Retornar estado actualizado ────────────────────────────────────
        return await mediator.Send(new GetEstadoOcrQuery(command.IdImagen), ct);
    }

    private sealed record ImagenRow(Guid Id, string UrlBlobRaw, string EstadoImagen);
}
