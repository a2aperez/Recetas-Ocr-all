using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Ocr;
using RecetasOCR.Application.DTOs.Paginacion;

namespace RecetasOCR.Application.Features.Ocr;

public record GetColaOcrQuery(
    string? EstadoCola = null,
    int     Page       = 1,
    int     PageSize   = 20
) : IRequest<PagedResultDto<ColaOcrItemDto>>;

public class GetColaOcrQueryHandler(
    IRecetasOcrDbContext             db,
    ILogger<GetColaOcrQueryHandler>  logger)
    : IRequestHandler<GetColaOcrQuery, PagedResultDto<ColaOcrItemDto>>
{
    public async Task<PagedResultDto<ColaOcrItemDto>> Handle(
        GetColaOcrQuery   query,
        CancellationToken ct)
    {
        var page     = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var offset   = (page - 1) * pageSize;
        var estado   = query.EstadoCola;

        try
        {
            var total = await db.Database
                .SqlQuery<int>($"""
                    SELECT COUNT(*) AS Value
                    FROM   ocr.ColaProcesamiento
                    WHERE  ({estado} IS NULL OR EstadoCola = {estado})
                    """)
                .FirstOrDefaultAsync(ct);

            if (total == 0)
                return PagedResultDto<ColaOcrItemDto>.Empty(page, pageSize);

            var rows = await db.Database
                .SqlQuery<ColaOcrRow>($"""
                    SELECT c.Id, c.IdImagen,
                           i.NombreArchivo,
                           c.EstadoCola, c.Prioridad, c.Intentos, c.MaxIntentos,
                           c.Bloqueado, c.WorkerProcesando,
                           c.FechaEncolado, c.FechaInicioProceso
                    FROM   ocr.ColaProcesamiento c
                    INNER  JOIN rec.Imagenes i ON i.Id = c.IdImagen
                    WHERE  ({estado} IS NULL OR c.EstadoCola = {estado})
                    ORDER  BY c.Prioridad ASC, c.FechaEncolado ASC
                    OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
                    """)
                .ToListAsync(ct);

            var items = rows
                .Select(r => new ColaOcrItemDto(
                    r.Id, r.IdImagen, r.NombreArchivo, r.EstadoCola,
                    r.Prioridad, r.Intentos, r.MaxIntentos, r.Bloqueado,
                    r.WorkerProcesando, r.FechaEncolado, r.FechaInicioProceso))
                .ToList();

            return new PagedResultDto<ColaOcrItemDto>(items, total, page, pageSize);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error al consultar cola OCR (EstadoCola={Estado}, Page={Page}). " +
                "Retornando resultado vacío para no bloquear el dashboard.",
                estado, page);
            return PagedResultDto<ColaOcrItemDto>.Empty(page, pageSize);
        }
    }

    private sealed record ColaOcrRow(
        long      Id,
        Guid      IdImagen,
        string?   NombreArchivo,
        string    EstadoCola,
        int       Prioridad,
        int       Intentos,
        int       MaxIntentos,
        bool      Bloqueado,
        string?   WorkerProcesando,
        DateTime  FechaEncolado,
        DateTime? FechaInicioProceso);
}
