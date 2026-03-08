using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Imagenes;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Imagenes;

/// <summary>
/// Query para obtener todas las imágenes de un grupo ordenadas por NumeroHoja.
/// Lanza EntidadNoEncontradaException si el grupo no existe → HTTP 404.
/// Permisos requeridos: IMAGENES.VER (validado en el controller).
/// </summary>
public record GetImagenesPorGrupoQuery(Guid IdGrupo) : IRequest<List<ImagenDto>>;

public class GetImagenesPorGrupoQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetImagenesPorGrupoQuery, List<ImagenDto>>
{
    public async Task<List<ImagenDto>> Handle(
        GetImagenesPorGrupoQuery query,
        CancellationToken        cancellationToken)
    {
        // Verificar que el grupo existe
        var grupoExiste = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(1) AS Value FROM rec.GruposReceta WHERE Id = {query.IdGrupo}
                """)
            .FirstAsync(cancellationToken);

        if (grupoExiste == 0)
            throw new EntidadNoEncontradaException("GrupoReceta", query.IdGrupo);

        var rows = await db.Database
            .SqlQuery<ImagenRow>($"""
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
                WHERE  i.IdGrupo = {query.IdGrupo}
                ORDER BY i.NumeroHoja
                """)
            .ToListAsync(cancellationToken);

        return rows.Select(MapToDto).ToList();
    }

    internal static ImagenDto MapToDto(ImagenRow r) => new(
        Id:               r.Id,
        IdGrupo:          r.IdGrupo,
        NumeroHoja:       r.NumeroHoja,
        UrlBlobRaw:       r.UrlBlobRaw,
        UrlBlobOcr:       r.UrlBlobOcr,
        UrlBlobIlegible:  r.UrlBlobIlegible,
        OrigenImagen:     r.OrigenImagen,
        NombreArchivo:    r.NombreArchivo,
        TamanioBytes:     r.TamanioBytes,
        FechaSubida:      r.FechaSubida,
        ScoreLegibilidad: r.ScoreLegibilidad,
        EsLegible:        r.EsLegible,
        MotivoBajaCalidad:r.MotivoBajaCalidad,
        EsCapturaManual:  r.EsCapturaManual,
        EstadoImagen:     r.EstadoClave,
        ModificadoPor:    r.ModificadoPor,
        FechaModificacion:r.FechaModificacion
    );

    // Tipo local que mapea el resultado del SELECT a campos de ImagenDto.
    // EstadoClave viene de cat.EstadosImagen.Clave vía JOIN.
    internal sealed record ImagenRow(
        Guid     Id,
        Guid     IdGrupo,
        int      NumeroHoja,
        string   UrlBlobRaw,
        string?  UrlBlobOcr,
        string?  UrlBlobIlegible,
        string   OrigenImagen,
        string   NombreArchivo,
        long?    TamanioBytes,
        DateTime FechaSubida,
        decimal? ScoreLegibilidad,
        bool?    EsLegible,
        string?  MotivoBajaCalidad,
        bool     EsCapturaManual,
        string   EstadoClave,
        string?  ModificadoPor,
        DateTime FechaModificacion
    );
}
