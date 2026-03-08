using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Imagenes;

public record ImagenEstadoOcrDto(
    Guid     IdImagen,
    string   EstadoImagen,
    bool?    EsLegible,
    decimal? ScoreLegibilidad,
    string?  MotivoBajaCalidad,
    string?  UrlBlobOcr,
    string?  UrlBlobIlegible,
    DateTime FechaModificacion
);

public record GetEstadoOcrQuery(Guid Id) : IRequest<ImagenEstadoOcrDto>;

public class GetEstadoOcrQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetEstadoOcrQuery, ImagenEstadoOcrDto>
{
    public async Task<ImagenEstadoOcrDto> Handle(GetEstadoOcrQuery query, CancellationToken ct)
    {
        var row = await db.Database
            .SqlQuery<OcrEstadoRow>($"""
                SELECT i.Id,
                       e.Clave          AS EstadoClave,
                       i.EsLegible,
                       i.ScoreLegibilidad,
                       i.MotivoBajaCalidad,
                       i.UrlBlobOCR     AS UrlBlobOcr,
                       i.UrlBlobIlegible,
                       i.FechaModificacion
                FROM   rec.Imagenes       i
                INNER JOIN cat.EstadosImagen e ON e.Id = i.IdEstadoImagen
                WHERE  i.Id = {query.Id}
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntidadNoEncontradaException("Imagen", query.Id);

        return new ImagenEstadoOcrDto(
            row.Id, row.EstadoClave, row.EsLegible,
            row.ScoreLegibilidad, row.MotivoBajaCalidad,
            row.UrlBlobOcr, row.UrlBlobIlegible,
            row.FechaModificacion
        );
    }

    private sealed record OcrEstadoRow(
        Guid     Id,
        string   EstadoClave,
        bool?    EsLegible,
        decimal? ScoreLegibilidad,
        string?  MotivoBajaCalidad,
        string?  UrlBlobOcr,
        string?  UrlBlobIlegible,
        DateTime FechaModificacion
    );
}
