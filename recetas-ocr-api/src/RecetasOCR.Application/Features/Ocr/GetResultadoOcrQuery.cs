using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Ocr;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Ocr;

public record GetResultadoOcrQuery(Guid IdImagen) : IRequest<ResultadoOcrDetalleDto>;

public class GetResultadoOcrQueryHandler(IRecetasOcrDbContext db)
    : IRequestHandler<GetResultadoOcrQuery, ResultadoOcrDetalleDto>
{
    public async Task<ResultadoOcrDetalleDto> Handle(
        GetResultadoOcrQuery query,
        CancellationToken    ct)
    {
        // Verificar imagen y obtener su estado
        var imagen = await db.Database
            .SqlQuery<ImagenRow>($"""
                SELECT i.Id,
                       e.Clave AS EstadoImagen,
                       i.EsLegible, i.MotivoBajaCalidad
                FROM   rec.Imagenes       i
                INNER  JOIN cat.EstadosImagen e ON e.Id = i.IdEstadoImagen
                WHERE  i.Id = {query.IdImagen}
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntidadNoEncontradaException("Imagen", query.IdImagen);

        // Resultado OCR más reciente — obligatorio para este endpoint
        var resultado = await db.Database
            .SqlQuery<ResultadoRow>($"""
                SELECT TOP 1
                       ConfianzaPromedio, ProveedorOCR AS ProveedorOcr,
                       ModeloUsado, DuracionMs, Exitoso,
                       ResponseJsonCompleto, TextoCompleto
                FROM   ocr.ResultadosOCR
                WHERE  IdImagen = {query.IdImagen}
                ORDER  BY Id DESC
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntidadNoEncontradaException("ResultadoOCR", query.IdImagen);

        // Extracción estructurada más reciente (puede no existir)
        var extraccion = await db.Database
            .SqlQuery<ExtraccionRow>($"""
                SELECT TOP 1
                       CamposFaltantes, AseguradoraDetectada,
                       FormatoDetectado, TokensEntrada, TokensSalida,
                       CostoEstimadoUSD AS CostoEstimadoUsd
                FROM   ocr.ResultadosExtraccion
                WHERE  IdImagen = {query.IdImagen}
                ORDER  BY Id DESC
                """)
            .FirstOrDefaultAsync(ct);

        // Cola más reciente (puede no existir)
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

        return new ResultadoOcrDetalleDto(
            IdImagen:           imagen.Id,
            EstadoImagen:       imagen.EstadoImagen,
            EstadoCola:         cola?.EstadoCola,
            Intentos:           cola?.Intentos,
            MaxIntentos:        cola?.MaxIntentos,
            Bloqueado:          cola?.Bloqueado,
            FechaEncolado:      cola?.FechaEncolado,
            FechaInicioProceso: cola?.FechaInicioProceso,
            FechaFinProceso:    cola?.FechaFinProceso,
            ConfianzaPromedio:  resultado.ConfianzaPromedio,
            EsLegible:          imagen.EsLegible,
            MotivoBajaCalidad:  imagen.MotivoBajaCalidad,
            ProveedorOcr:       resultado.ProveedorOcr,
            ModeloUsado:        resultado.ModeloUsado,
            DuracionMs:         resultado.DuracionMs,
            Exitoso:            resultado.Exitoso,
            ResponseJsonCompleto: resultado.ResponseJsonCompleto,
            TextoCompleto:      resultado.TextoCompleto,
            CamposFaltantes:    extraccion?.CamposFaltantes,
            AseguradoraDetectada: extraccion?.AseguradoraDetectada,
            FormatoDetectado:   extraccion?.FormatoDetectado,
            TokensEntrada:      extraccion?.TokensEntrada,
            TokensSalida:       extraccion?.TokensSalida,
            CostoEstimadoUsd:   extraccion?.CostoEstimadoUsd);
    }

    private sealed record ImagenRow(Guid Id, string EstadoImagen, bool? EsLegible, string? MotivoBajaCalidad);

    private sealed record ResultadoRow(
        decimal?  ConfianzaPromedio,
        string    ProveedorOcr,
        string?   ModeloUsado,
        int?      DuracionMs,
        bool      Exitoso,
        string?   ResponseJsonCompleto,
        string?   TextoCompleto);

    private sealed record ExtraccionRow(
        string?  CamposFaltantes,
        string?  AseguradoraDetectada,
        string?  FormatoDetectado,
        int?     TokensEntrada,
        int?     TokensSalida,
        decimal? CostoEstimadoUsd);

    private sealed record ColaRow(
        string    EstadoCola,
        int       Intentos,
        int       MaxIntentos,
        bool      Bloqueado,
        DateTime  FechaEncolado,
        DateTime? FechaInicioProceso,
        DateTime? FechaFinProceso);
}
