using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Imagenes;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Imagenes;

/// <summary>
/// Query para obtener una imagen por su Id.
/// Lanza EntidadNoEncontradaException si no existe → HTTP 404.
/// Permisos requeridos: IMAGENES.VER (validado en el controller).
/// </summary>
public record GetImagenByIdQuery(Guid Id) : IRequest<ImagenDto>;

public class GetImagenByIdQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetImagenByIdQuery, ImagenDto>
{
    public async Task<ImagenDto> Handle(
        GetImagenByIdQuery query,
        CancellationToken  cancellationToken)
    {
        var row = await db.Database
            .SqlQuery<GetImagenesPorGrupoQueryHandler.ImagenRow>($"""
                SELECT
                    i.Id,
                    i.IdGrupo,
                    i.NumeroHoja,
                    i.UrlBlobRaw,
                    i.UrlBlobOCR        AS UrlBlobOcr,
                    i.UrlBlobIlegible,
                    i.OrigenImagen,
                    i.NombreArchivo,
                    i.TamanioBytes,
                    i.FechaSubida,
                    i.ScoreLegibilidad,
                    i.EsLegible,
                    i.MotivoBajaCalidad,
                    i.EsCapturaManual,
                    e.Clave             AS EstadoClave,
                    i.ModificadoPor,
                    i.FechaModificacion
                FROM   rec.Imagenes       i
                INNER JOIN cat.EstadosImagen e ON e.Id = i.IdEstadoImagen
                WHERE  i.Id = {query.Id}
                """)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new EntidadNoEncontradaException("Imagen", query.Id);

        return GetImagenesPorGrupoQueryHandler.MapToDto(row);
    }
}
