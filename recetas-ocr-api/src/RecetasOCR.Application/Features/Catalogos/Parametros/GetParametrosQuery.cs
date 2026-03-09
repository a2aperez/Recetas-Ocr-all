using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Catalogos;

namespace RecetasOCR.Application.Features.Catalogos.Parametros;

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Retorna todos los parámetros de cfg.Parametros incluyendo descripción.
/// Los parámetros marcados como EsSecreto muestran "***" como valor.
/// </summary>
public record GetParametrosQuery : IRequest<List<ParametroDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetParametrosQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetParametrosQuery, List<ParametroDto>>
{
    public async Task<List<ParametroDto>> Handle(
        GetParametrosQuery _,
        CancellationToken  ct)
    {
        var rows = await db.Database
            .SqlQuery<ParametroRow>($"""
                SELECT Id, Clave, Valor, Descripcion, Tipo, EsSecreto
                FROM   cfg.Parametros
                ORDER  BY Clave ASC
                """)
            .ToListAsync(ct);

        return rows
            .Select(r => new ParametroDto(
                r.Id,
                r.Clave,
                r.EsSecreto ? "***" : r.Valor,
                r.Descripcion,
                r.Tipo,
                r.EsSecreto))
            .ToList();
    }

    private sealed record ParametroRow(
        int     Id,
        string  Clave,
        string  Valor,
        string? Descripcion,
        string  Tipo,
        bool    EsSecreto);
}
