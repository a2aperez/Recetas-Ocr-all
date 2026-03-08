using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Paginacion;
using RecetasOCR.Domain.Common;

namespace RecetasOCR.Application.Features.Revision;

// ──────────────────────────────────────────────────────────────────────────────
// DTO de cola
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Ítem de la cola de revisión humana: datos de la imagen y del grupo asociado.
/// Se usa en GET /api/revision/cola.
/// </summary>
public record ColaRevisionItemDto(
    // Imagen
    Guid     IdImagen,
    int      NumeroHoja,
    string   UrlBlobRaw,
    string?  UrlBlobOcr,
    string   EstadoImagen,
    decimal? ScoreLegibilidad,
    bool?    EsLegible,
    string?  MotivoBajaCalidad,
    DateTime FechaSubida,
    // Grupo
    Guid     IdGrupo,
    string?  FolioBase,
    string?  NombrePaciente,
    DateOnly? FechaConsulta,
    string   EstadoGrupo,
    int      TotalImagenes,
    // Asignación (si existe)
    Guid?    IdAsignacion,
    Guid?    IdUsuarioAsignado,
    DateTime? FechaLimite,
    int      Prioridad
);

// ──────────────────────────────────────────────────────────────────────────────
// Query
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Obtiene la cola de imágenes pendientes de revisión humana, paginada.
/// Filtra imágenes en EstadosValidos.ImagenesPendientesRevision() ordenadas por:
///   1. Prioridad asignación DESC (PENDING sin asignación al final)
///   2. FechaSubida ASC (más antiguas primero)
/// </summary>
public record GetColaRevisionQuery(
    int Page     = 1,
    int PageSize = 20
) : IRequest<PagedResultDto<ColaRevisionItemDto>>;

// ──────────────────────────────────────────────────────────────────────────────
// Handler
// ──────────────────────────────────────────────────────────────────────────────

public class GetColaRevisionQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetColaRevisionQuery, PagedResultDto<ColaRevisionItemDto>>
{
    // Claves de cat.EstadosImagen que están pendientes de revisión humana.
    // Refleja EstadosValidos.ImagenesPendientesRevision().
    private static readonly string[] _estadosPendientes =
        EstadosValidos.ImagenesPendientesRevision()
            .Select(e => e.ToString().ToUpperInvariant())
            .ToArray();

    public async Task<PagedResultDto<ColaRevisionItemDto>> Handle(
        GetColaRevisionQuery query,
        CancellationToken    cancellationToken)
    {
        var page     = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var offset   = (page - 1) * pageSize;

        // Los parámetros IN no se pueden parametrizar directamente con SqlQuery;
        // construimos la lista de claves como literal seguro (son strings del enum).
        var clavesSql = string.Join(",", _estadosPendientes.Select(c => $"'{c}'"));

        // ── Total ──────────────────────────────────────────────────────
        var total = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value
                FROM   rec.Imagenes       i
                INNER JOIN cat.EstadosImagen ei ON ei.Id = i.IdEstadoImagen
                WHERE  ei.Clave IN ({clavesSql})
                """)
            .FirstAsync(cancellationToken);

        if (total == 0)
            return PagedResultDto<ColaRevisionItemDto>.Empty(page, pageSize);

        // ── Página ─────────────────────────────────────────────────────
        var rows = await db.Database
            .SqlQuery<ColaRow>($"""
                SELECT
                    -- imagen
                    i.Id              AS IdImagen,
                    i.NumeroHoja,
                    i.UrlBlobRaw,
                    i.UrlBlobOCR      AS UrlBlobOcr,
                    ei.Clave          AS EstadoImagen,
                    i.ScoreLegibilidad,
                    i.EsLegible,
                    i.MotivoBajaCalidad,
                    i.FechaSubida,
                    -- grupo
                    g.Id              AS IdGrupo,
                    g.FolioBase,
                    g.NombrePaciente,
                    g.FechaConsulta,
                    eg.Clave          AS EstadoGrupo,
                    g.TotalImagenes,
                    -- asignación (LEFT JOIN — puede no existir)
                    ar.Id             AS IdAsignacion,
                    ar.IdUsuarioAsignado,
                    ar.FechaLimite,
                    COALESCE(ar.Prioridad, 0) AS Prioridad
                FROM   rec.Imagenes           i
                INNER JOIN cat.EstadosImagen  ei ON ei.Id = i.IdEstadoImagen
                INNER JOIN rec.GruposReceta    g  ON g.Id  = i.IdGrupo
                INNER JOIN cat.EstadosGrupo   eg ON eg.Id = g.IdEstadoGrupo
                LEFT  JOIN rev.AsignacionesRevision ar
                       ON  ar.IdImagen = i.Id AND ar.Estado = 'PENDIENTE'
                WHERE  ei.Clave IN ({clavesSql})
                ORDER BY COALESCE(ar.Prioridad, 0) DESC, i.FechaSubida ASC
                OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
                """)
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new ColaRevisionItemDto(
            IdImagen:          r.IdImagen,
            NumeroHoja:        r.NumeroHoja,
            UrlBlobRaw:        r.UrlBlobRaw,
            UrlBlobOcr:        r.UrlBlobOcr,
            EstadoImagen:      r.EstadoImagen,
            ScoreLegibilidad:  r.ScoreLegibilidad,
            EsLegible:         r.EsLegible,
            MotivoBajaCalidad: r.MotivoBajaCalidad,
            FechaSubida:       r.FechaSubida,
            IdGrupo:           r.IdGrupo,
            FolioBase:         r.FolioBase,
            NombrePaciente:    r.NombrePaciente,
            FechaConsulta:     r.FechaConsulta,
            EstadoGrupo:       r.EstadoGrupo,
            TotalImagenes:     r.TotalImagenes,
            IdAsignacion:      r.IdAsignacion,
            IdUsuarioAsignado: r.IdUsuarioAsignado,
            FechaLimite:       r.FechaLimite,
            Prioridad:         r.Prioridad
        )).ToList();

        return new PagedResultDto<ColaRevisionItemDto>(items, total, page, pageSize);
    }

    // ── Tipo local para resultado del SELECT ────────────────────────────
    private sealed record ColaRow(
        Guid      IdImagen,
        int       NumeroHoja,
        string    UrlBlobRaw,
        string?   UrlBlobOcr,
        string    EstadoImagen,
        decimal?  ScoreLegibilidad,
        bool?     EsLegible,
        string?   MotivoBajaCalidad,
        DateTime  FechaSubida,
        Guid      IdGrupo,
        string?   FolioBase,
        string?   NombrePaciente,
        DateOnly? FechaConsulta,
        string    EstadoGrupo,
        int       TotalImagenes,
        Guid?     IdAsignacion,
        Guid?     IdUsuarioAsignado,
        DateTime? FechaLimite,
        int       Prioridad
    );
}
