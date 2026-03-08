using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.GruposReceta;
using RecetasOCR.Application.DTOs.Paginacion;

namespace RecetasOCR.Application.Features.GruposReceta;

/// <summary>
/// Query para listar grupos de receta con filtros y paginación.
/// Todos los filtros son opcionales.
/// Busqueda aplica LIKE sobre FolioBase, NombrePaciente y NombreMedico.
/// Ordenado por FechaCreacion DESC (más recientes primero).
/// </summary>
public record GetGruposRecetaQuery(FiltrosGrupoDto Filtros)
    : IRequest<PagedResultDto<GrupoRecetaDto>>;

public class GetGruposRecetaQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetGruposRecetaQuery, PagedResultDto<GrupoRecetaDto>>
{
    public async Task<PagedResultDto<GrupoRecetaDto>> Handle(
        GetGruposRecetaQuery query,
        CancellationToken    cancellationToken)
    {
        var f        = query.Filtros;
        var page     = Math.Max(1, f.Page);
        var pageSize = Math.Clamp(f.PageSize, 1, 100);
        var offset   = (page - 1) * pageSize;

        // Variables locales para que el interpolador de FormattableString
        // las convierta en SqlParameter. Los IS NULL permiten filtros opcionales.
        var idAseguradora = f.IdAseguradora;
        var estadoGrupo   = f.EstadoGrupo;
        var fechaDesde    = f.FechaDesde;
        var fechaHasta    = f.FechaHasta;
        var busquedaLike  = f.Busqueda != null ? $"%{f.Busqueda}%" : null;

        // ── Total ──────────────────────────────────────────────────────
        var total = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value
                FROM   rec.GruposReceta     g
                INNER JOIN cat.EstadosGrupo eg ON eg.Id = g.IdEstadoGrupo
                WHERE  ({idAseguradora} IS NULL OR g.IdAseguradora  = {idAseguradora})
                  AND  ({estadoGrupo}   IS NULL OR eg.Clave         = {estadoGrupo})
                  AND  ({fechaDesde}    IS NULL OR g.FechaCreacion >= {fechaDesde})
                  AND  ({fechaHasta}    IS NULL OR g.FechaCreacion <= {fechaHasta})
                  AND  ({busquedaLike}  IS NULL
                        OR g.FolioBase      LIKE {busquedaLike}
                        OR g.NombrePaciente LIKE {busquedaLike}
                        OR g.NombreMedico   LIKE {busquedaLike})
                """)
            .FirstAsync(cancellationToken);

        if (total == 0)
            return PagedResultDto<GrupoRecetaDto>.Empty(page, pageSize);

        // ── Página ─────────────────────────────────────────────────────
        var rows = await db.Database
            .SqlQuery<GrupoRow>($"""
                SELECT
                    g.Id, g.FolioBase, g.IdCliente, g.IdAseguradora,
                    a.Nombre              AS NombreAseguradora,
                    g.Nur, g.NombrePaciente, g.ApellidoPaterno, g.ApellidoMaterno,
                    g.NombreMedico, g.CedulaMedico, g.EspecialidadTexto,
                    g.CodigoCIE10         AS CodigoCie10,
                    g.DescripcionDiagnostico, g.FechaConsulta,
                    g.TotalImagenes, g.TotalMedicamentos,
                    eg.Clave              AS EstadoGrupo,
                    g.FechaCreacion, g.FechaActualizacion,
                    g.ModificadoPor, g.FechaModificacion
                FROM   rec.GruposReceta     g
                INNER JOIN cat.EstadosGrupo  eg ON eg.Id = g.IdEstadoGrupo
                INNER JOIN cat.Aseguradoras  a  ON a.Id  = g.IdAseguradora
                WHERE  ({idAseguradora} IS NULL OR g.IdAseguradora  = {idAseguradora})
                  AND  ({estadoGrupo}   IS NULL OR eg.Clave         = {estadoGrupo})
                  AND  ({fechaDesde}    IS NULL OR g.FechaCreacion >= {fechaDesde})
                  AND  ({fechaHasta}    IS NULL OR g.FechaCreacion <= {fechaHasta})
                  AND  ({busquedaLike}  IS NULL
                        OR g.FolioBase      LIKE {busquedaLike}
                        OR g.NombrePaciente LIKE {busquedaLike}
                        OR g.NombreMedico   LIKE {busquedaLike})
                ORDER BY g.FechaCreacion DESC
                OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
                """)
            .ToListAsync(cancellationToken);

        return new(rows.Select(MapToDto).ToList(), total, page, pageSize);
    }

    // ── Helpers compartidos con GetGrupoRecetaDetalleQuery ─────────────

    internal static GrupoRecetaDto MapToDto(GrupoRow r) => new(
        Id:                     r.Id,
        FolioBase:              r.FolioBase,
        IdCliente:              r.IdCliente,
        IdAseguradora:          r.IdAseguradora,
        NombreAseguradora:      r.NombreAseguradora,
        Nur:                    r.Nur,
        NombrePaciente:         r.NombrePaciente,
        ApellidoPaterno:        r.ApellidoPaterno,
        ApellidoMaterno:        r.ApellidoMaterno,
        NombreMedico:           r.NombreMedico,
        CedulaMedico:           r.CedulaMedico,
        EspecialidadTexto:      r.EspecialidadTexto,
        CodigoCie10:            r.CodigoCie10,
        DescripcionDiagnostico: r.DescripcionDiagnostico,
        FechaConsulta:          r.FechaConsulta,
        TotalImagenes:          r.TotalImagenes,
        TotalMedicamentos:      r.TotalMedicamentos,
        EstadoGrupo:            r.EstadoGrupo,
        FechaCreacion:          r.FechaCreacion,
        FechaActualizacion:     r.FechaActualizacion,
        ModificadoPor:          r.ModificadoPor,
        FechaModificacion:      r.FechaModificacion
    );

    // Tipo local que mapea el resultado del SELECT principal.
    // Compartido con GetGrupoRecetaDetalleQueryHandler.
    internal sealed record GrupoRow(
        Guid      Id,
        string?   FolioBase,
        Guid?     IdCliente,
        int       IdAseguradora,
        string?   NombreAseguradora,
        string?   Nur,
        string?   NombrePaciente,
        string?   ApellidoPaterno,
        string?   ApellidoMaterno,
        string?   NombreMedico,
        string?   CedulaMedico,
        string?   EspecialidadTexto,
        string?   CodigoCie10,
        string?   DescripcionDiagnostico,
        DateOnly? FechaConsulta,
        int       TotalImagenes,
        int       TotalMedicamentos,
        string    EstadoGrupo,
        DateTime  FechaCreacion,
        DateTime  FechaActualizacion,
        string?   ModificadoPor,
        DateTime  FechaModificacion
    );
}
