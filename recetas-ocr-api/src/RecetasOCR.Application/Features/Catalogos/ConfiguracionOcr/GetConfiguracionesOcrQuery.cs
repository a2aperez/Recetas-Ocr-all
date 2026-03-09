using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Catalogos;

namespace RecetasOCR.Application.Features.Catalogos.ConfiguracionOcr;

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Retorna todas las configuraciones OCR.
/// La ApiKeyEncriptada se muestra como los primeros 8 caracteres + "****"
/// para evitar exposición de credenciales completas.
/// </summary>
public record GetConfiguracionesOcrQuery : IRequest<List<ConfiguracionOcrDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetConfiguracionesOcrQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetConfiguracionesOcrQuery, List<ConfiguracionOcrDto>>
{
    public async Task<List<ConfiguracionOcrDto>> Handle(
        GetConfiguracionesOcrQuery _,
        CancellationToken          ct)
    {
        var rows = await db.Database
            .SqlQuery<ConfiguracionOcrRow>($"""
                SELECT Id, Nombre, Proveedor, UrlBase, ApiKeyEncriptada,
                       Modelo, Version, TimeoutSegundos, MaxReintentos,
                       CostoPorImagenUSD, EsPrincipal, Activo,
                       ConfigJson, FechaActualizacion
                FROM   cfg.ConfiguracionesOCR
                ORDER  BY EsPrincipal DESC, Nombre ASC
                """)
            .ToListAsync(ct);

        return rows
            .Select(r => new ConfiguracionOcrDto(
                r.Id,
                r.Nombre,
                r.Proveedor,
                r.UrlBase,
                MaskApiKey(r.ApiKeyEncriptada),
                r.Modelo,
                r.Version,
                r.TimeoutSegundos,
                r.MaxReintentos,
                r.CostoPorImagenUSD,
                r.EsPrincipal,
                r.Activo,
                r.ConfigJson,
                r.FechaActualizacion))
            .ToList();
    }

    private static string? MaskApiKey(string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        var visible = key.Length > 8 ? key[..8] : key;
        return visible + "****";
    }

    private sealed record ConfiguracionOcrRow(
        int      Id,
        string   Nombre,
        string   Proveedor,
        string   UrlBase,
        string?  ApiKeyEncriptada,
        string?  Modelo,
        string?  Version,
        int      TimeoutSegundos,
        int      MaxReintentos,
        decimal  CostoPorImagenUSD,
        bool     EsPrincipal,
        bool     Activo,
        string?  ConfigJson,
        DateTime FechaActualizacion);
}
