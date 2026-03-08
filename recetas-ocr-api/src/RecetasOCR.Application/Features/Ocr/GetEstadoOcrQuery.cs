using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Ocr;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Ocr;

public record GetEstadoOcrQuery(Guid IdImagen) : IRequest<EstadoOcrDto>;

public class GetEstadoOcrQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetEstadoOcrQuery, EstadoOcrDto>
{
    public async Task<EstadoOcrDto> Handle(
        GetEstadoOcrQuery query,
        CancellationToken ct)
    {
        // Verificar que la imagen existe
        var imagen = await db.Database
            .SqlQuery<ImagenRow>($"""
                SELECT i.Id,
                       e.Clave         AS EstadoImagen,
                       i.EsLegible,
                       i.MotivoBajaCalidad
                FROM   rec.Imagenes       i
                INNER  JOIN cat.EstadosImagen e ON e.Id = i.IdEstadoImagen
                WHERE  i.Id = {query.IdImagen}
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntidadNoEncontradaException("Imagen", query.IdImagen);

        // Registro más reciente en la cola (puede no existir)
        var cola = await db.Database
            .SqlQuery<ColaRow>($"""
                SELECT TOP 1
                       EstadoCola, Intentos, MaxIntentos, Bloqueado,
                       FechaEncolado, FechaInicioProceso, FechaFinProceso
                FROM   ocr.ColaProcesamiento
                WHERE  IdImagen = {query.IdImagen}
                ORDER  BY Id DESC
                """)
            .FirstOrDefaultAsync(ct);

        // Resultado OCR más reciente (puede no existir)
        var resultado = await db.Database
            .SqlQuery<ResultadoRow>($"""
                SELECT TOP 1
                       ConfianzaPromedio, ProveedorOCR AS ProveedorOcr,
                       ModeloUsado, DuracionMs, Exitoso
                FROM   ocr.ResultadosOCR
                WHERE  IdImagen = {query.IdImagen}
                ORDER  BY Id DESC
                """)
            .FirstOrDefaultAsync(ct);

        return new EstadoOcrDto(
            IdImagen:          imagen.Id,
            EstadoImagen:      imagen.EstadoImagen,
            EstadoCola:        cola?.EstadoCola,
            Intentos:          cola?.Intentos,
            MaxIntentos:       cola?.MaxIntentos,
            Bloqueado:         cola?.Bloqueado,
            FechaEncolado:     cola?.FechaEncolado,
            FechaInicioProceso: cola?.FechaInicioProceso,
            FechaFinProceso:   cola?.FechaFinProceso,
            ConfianzaPromedio: resultado?.ConfianzaPromedio,
            EsLegible:         imagen.EsLegible,
            MotivoBajaCalidad: imagen.MotivoBajaCalidad,
            ProveedorOcr:      resultado?.ProveedorOcr,
            ModeloUsado:       resultado?.ModeloUsado,
            DuracionMs:        resultado?.DuracionMs,
            Exitoso:           resultado?.Exitoso);
    }

    private sealed record ImagenRow(Guid Id, string EstadoImagen, bool? EsLegible, string? MotivoBajaCalidad);

    private sealed record ColaRow(
        string    EstadoCola,
        int       Intentos,
        int       MaxIntentos,
        bool      Bloqueado,
        DateTime  FechaEncolado,
        DateTime? FechaInicioProceso,
        DateTime? FechaFinProceso);

    private sealed record ResultadoRow(
        decimal? ConfianzaPromedio,
        string   ProveedorOcr,
        string?  ModeloUsado,
        int?     DuracionMs,
        bool     Exitoso);
}
