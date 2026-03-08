using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.GruposReceta;
using RecetasOCR.Application.DTOs.Imagenes;
using RecetasOCR.Application.DTOs.Medicamentos;
using RecetasOCR.Application.Features.Imagenes;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.GruposReceta;

/// <summary>
/// Query para obtener el detalle completo de un grupo de receta:
/// datos principales + todas sus imágenes + todos sus medicamentos.
/// Lanza EntidadNoEncontradaException si el grupo no existe → HTTP 404.
/// </summary>
public record GetGrupoRecetaDetalleQuery(Guid Id) : IRequest<GrupoRecetaDetalleDto>;

public class GetGrupoRecetaDetalleQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetGrupoRecetaDetalleQuery, GrupoRecetaDetalleDto>
{
    public async Task<GrupoRecetaDetalleDto> Handle(
        GetGrupoRecetaDetalleQuery query,
        CancellationToken          cancellationToken)
    {
        // ── 1. Grupo principal ─────────────────────────────────────────
        var grupo = await db.Database
            .SqlQuery<GetGruposRecetaQueryHandler.GrupoRow>($"""
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
                WHERE  g.Id = {query.Id}
                """)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new EntidadNoEncontradaException("GrupoReceta", query.Id);

        // ── 2. Imágenes ordenadas por NumeroHoja ───────────────────────
        // Reutiliza el ImagenRow y MapToDto de Features.Imagenes para
        // mantener consistencia con GetImagenesPorGrupoQuery.
        var imagenRows = await db.Database
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
                WHERE  i.IdGrupo = {query.Id}
                ORDER BY i.NumeroHoja
                """)
            .ToListAsync(cancellationToken);

        var imagenes = imagenRows
            .Select(GetImagenesPorGrupoQueryHandler.MapToDto)
            .ToList();

        // ── 3. Medicamentos ────────────────────────────────────────────
        // Campos ausentes en la entidad scaffolded se devuelven como NULL/false:
        //   Concentracion, FormaFarmaceutica → NULL (no existen en el modelo actual)
        //   ViaAdministracion → JOIN a cat.ViasAdministracion.Descripcion
        //   FueExtraido, ValidadoPorRevisor → CAST(0 AS BIT) por defecto
        //   ConfianzaExtraccion → NULL
        var medRows = await db.Database
            .SqlQuery<MedicamentoRow>($"""
                SELECT
                    m.Id, m.IdImagen, m.IdGrupo, m.IdMedicamentoCatalogo,
                    m.NumeroPrescripcion,
                    m.CodigoCIE10           AS CodigoCie10,
                    m.DescripcionCIE10      AS DescripcionCie10,
                    m.NombreComercial,
                    m.SustanciaActiva       AS NombreGenerico,
                    m.Presentacion,
                    CAST(NULL AS NVARCHAR(100))  AS Concentracion,
                    CAST(NULL AS NVARCHAR(100))  AS FormaFarmaceutica,
                    va.Descripcion          AS ViaAdministracion,
                    m.Dosis,
                    m.FrecuenciaTexto, m.FrecuenciaExpandida,
                    m.DuracionTexto, m.DuracionDias,
                    m.IndicacionesCompletas, m.NumeroAutorizacion,
                    CAST(0 AS BIT)              AS FueExtraido,
                    CAST(NULL AS DECIMAL(5,4))  AS ConfianzaExtraccion,
                    CAST(0 AS BIT)              AS ValidadoPorRevisor,
                    m.ModificadoPor, m.FechaModificacion
                FROM   med.MedicamentosReceta  m
                LEFT   JOIN cat.ViasAdministracion va ON va.Id = m.IdViaAdministracion
                WHERE  m.IdGrupo = {query.Id}
                ORDER BY m.NumeroPrescripcion
                """)
            .ToListAsync(cancellationToken);

        var medicamentos = medRows.Select(MapMedToDto).ToList();

        // ── 4. Construir detalle ───────────────────────────────────────
        var grupoDto = GetGruposRecetaQueryHandler.MapToDto(grupo);

        return new GrupoRecetaDetalleDto(
            Id:                     grupoDto.Id,
            FolioBase:              grupoDto.FolioBase,
            IdCliente:              grupoDto.IdCliente,
            IdAseguradora:          grupoDto.IdAseguradora,
            NombreAseguradora:      grupoDto.NombreAseguradora,
            Nur:                    grupoDto.Nur,
            NombrePaciente:         grupoDto.NombrePaciente,
            ApellidoPaterno:        grupoDto.ApellidoPaterno,
            ApellidoMaterno:        grupoDto.ApellidoMaterno,
            NombreMedico:           grupoDto.NombreMedico,
            CedulaMedico:           grupoDto.CedulaMedico,
            EspecialidadTexto:      grupoDto.EspecialidadTexto,
            CodigoCie10:            grupoDto.CodigoCie10,
            DescripcionDiagnostico: grupoDto.DescripcionDiagnostico,
            FechaConsulta:          grupoDto.FechaConsulta,
            TotalImagenes:          grupoDto.TotalImagenes,
            TotalMedicamentos:      grupoDto.TotalMedicamentos,
            EstadoGrupo:            grupoDto.EstadoGrupo,
            FechaCreacion:          grupoDto.FechaCreacion,
            FechaActualizacion:     grupoDto.FechaActualizacion,
            ModificadoPor:          grupoDto.ModificadoPor,
            FechaModificacion:      grupoDto.FechaModificacion,
            Imagenes:               imagenes,
            Medicamentos:           medicamentos
        );
    }

    private static MedicamentoRecetaDto MapMedToDto(MedicamentoRow m) => new(
        Id:                  m.Id,
        IdImagen:            m.IdImagen,
        IdGrupo:             m.IdGrupo,
        IdMedicamentoCatalogo: m.IdMedicamentoCatalogo,
        NumeroPrescripcion:  m.NumeroPrescripcion,
        CodigoCie10:         m.CodigoCie10,
        DescripcionCie10:    m.DescripcionCie10,
        NombreComercial:     m.NombreComercial,
        NombreGenerico:      m.NombreGenerico,
        Presentacion:        m.Presentacion,
        Concentracion:       m.Concentracion,
        FormaFarmaceutica:   m.FormaFarmaceutica,
        ViaAdministracion:   m.ViaAdministracion,
        Dosis:               m.Dosis,
        FrecuenciaTexto:     m.FrecuenciaTexto,
        FrecuenciaExpandida: m.FrecuenciaExpandida,
        DuracionTexto:       m.DuracionTexto,
        DuracionDias:        m.DuracionDias,
        IndicacionesCompletas:m.IndicacionesCompletas,
        NumeroAutorizacion:  m.NumeroAutorizacion,
        FueExtraido:         m.FueExtraido,
        ConfianzaExtraccion: m.ConfianzaExtraccion,
        ValidadoPorRevisor:  m.ValidadoPorRevisor,
        ModificadoPor:       m.ModificadoPor,
        FechaModificacion:   m.FechaModificacion
    );

    private sealed record MedicamentoRow(
        Guid     Id,
        Guid     IdImagen,
        Guid     IdGrupo,
        int?     IdMedicamentoCatalogo,
        int      NumeroPrescripcion,
        string?  CodigoCie10,
        string?  DescripcionCie10,
        string?  NombreComercial,
        string?  NombreGenerico,
        string?  Presentacion,
        string?  Concentracion,
        string?  FormaFarmaceutica,
        string?  ViaAdministracion,
        string?  Dosis,
        string?  FrecuenciaTexto,
        string?  FrecuenciaExpandida,
        string?  DuracionTexto,
        int?     DuracionDias,
        string?  IndicacionesCompletas,
        string?  NumeroAutorizacion,
        bool     FueExtraido,
        decimal? ConfianzaExtraccion,
        bool     ValidadoPorRevisor,
        string?  ModificadoPor,
        DateTime FechaModificacion
    );
}
