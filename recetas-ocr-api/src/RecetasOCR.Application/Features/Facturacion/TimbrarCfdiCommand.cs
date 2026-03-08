using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Facturacion;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Facturacion;

// ── Command ───────────────────────────────────────────────────────────────────

public record TimbrarCfdiCommand(Guid IdPreFactura) : IRequest<CfdiDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class TimbrarCfdiCommandHandler(
    IRecetasOcrDbContext db,
    IPacService          pac,
    IBlobStorageService  blob,
    ICurrentUserService  currentUser,
    ILogger<TimbrarCfdiCommandHandler> logger)
    : IRequestHandler<TimbrarCfdiCommand, CfdiDto>
{
    public async Task<CfdiDto> Handle(TimbrarCfdiCommand command, CancellationToken ct)
    {
        var ahora    = DateTime.UtcNow;
        var username = currentUser.Username ?? "sistema";
        var userId   = currentUser.UserId;

        // ── Cargar PreFactura con datos del emisor/receptor ───────────────────
        var rows = await db.Database
            .SqlQuery<PreFacturaTimbradoRow>($"""
                SELECT pf.Id, pf.IdGrupo, pf.Estado, pf.Total,
                       pf.Subtotal, pf.TotalIVA, pf.Version,
                       pf.TipoComprobante, pf.IntentosTimbrado,
                       e.RFC  AS RFCEmisor,  e.RazonSocial  AS NombreEmisor,
                       r.RFC  AS RFCReceptor, r.NombreRazonSocial AS NombreReceptor
                FROM   fac.PreFacturas  pf
                INNER JOIN fac.Emisores   e ON e.Id = pf.IdEmisor
                INNER JOIN fac.Receptores r ON r.Id = pf.IdReceptor
                WHERE  pf.Id = {command.IdPreFactura}
                """)
            .ToListAsync(ct);

        var pf = rows.FirstOrDefault()
            ?? throw new EntidadNoEncontradaException("PreFactura", command.IdPreFactura);

        if (pf.Estado != "BORRADOR" && pf.Estado != "PENDIENTE_TIMBRADO")
            throw new EstadoInvalidoException("PreFactura", pf.Estado, ["BORRADOR", "PENDIENTE_TIMBRADO"]);

        // ── Construir XML CFDI 4.0 simplificado ──────────────────────────────
        var xmlCfdi = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <cfdi:Comprobante xmlns:cfdi="http://www.sat.gob.mx/cfd/4"
              Version="{pf.Version}" Folio="{pf.Id:N}"
              TipoDeComprobante="{pf.TipoComprobante}"
              Total="{pf.Total:F2}" SubTotal="{pf.Subtotal:F2}"
              Moneda="MXN" Exportacion="01"/>
            """;

        // ── Llamar al PAC ─────────────────────────────────────────────────────
        var resultado = await pac.TimbrarAsync(xmlCfdi, ct);

        if (resultado.Exitoso)
            return await ProcesarExito(pf, resultado, ahora, username, userId, ct);

        await ProcesarError(pf, resultado, ahora, username, userId, ct);
        throw new InvalidOperationException(
            $"Error al timbrar CFDI: {resultado.MensajeError}");
    }

    private async Task<CfdiDto> ProcesarExito(
        PreFacturaTimbradoRow pf,
        PacResultadoDto       resultado,
        DateTime              ahora,
        string                username,
        Guid?                 userId,
        CancellationToken     ct)
    {
        // Subir XML
        var xmlBytes = System.Text.Encoding.UTF8.GetBytes(resultado.XmlTimbrado ?? string.Empty);
        using var xmlStream = new MemoryStream(xmlBytes);
        var urlXml = await blob.SubirCfdiXmlAsync(xmlStream, $"cfdi_{resultado.UUID}.xml", ct);

        // Subir PDF (stub: same bytes, log if fails)
        string? urlPdf = null;
        try
        {
            using var pdfStream = new MemoryStream(xmlBytes);
            urlPdf = await blob.SubirCfdiPdfAsync(pdfStream, $"cfdi_{resultado.UUID}.pdf", ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Timbrado] PDF no generado para UUID: {UUID}", resultado.UUID);
        }

        // INSERT fac.CFDI
        var cfdiId      = Guid.NewGuid();
        var uuid        = resultado.UUID!;
        var selloSat    = resultado.SelloSAT;
        var cadena      = resultado.CadenaOriginal;
        var noCert      = resultado.NoCertificadoSAT;

        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO fac.CFDI
                (Id, IdPreFactura, IdGrupo, UUID, FechaTimbrado, Version,
                 RFCEmisor, NombreEmisor, RFCReceptor, NombreReceptor, Total,
                 SelloSAT, CadenaOriginalSAT, NoCertificadoSAT,
                 UrlBlobXML, UrlBlobPDF, Estado,
                 FechaCreacion, ModificadoPor, FechaModificacion)
            VALUES
                ({cfdiId}, {pf.Id}, {pf.IdGrupo}, {uuid}, {ahora}, {pf.Version},
                 {pf.RFCEmisor}, {pf.NombreEmisor}, {pf.RFCReceptor}, {pf.NombreReceptor},
                 {pf.Total}, {selloSat}, {cadena}, {noCert},
                 {urlXml}, {urlPdf}, 'VIGENTE',
                 {ahora}, {username}, {ahora})
            """, ct);

        // UPDATE PreFactura → TIMBRADA
        await db.Database.ExecuteSqlAsync($"""
            UPDATE fac.PreFacturas
            SET    Estado = 'TIMBRADA', FechaAprobacion = {ahora},
                   IntentosTimbrado = IntentosTimbrado + 1,
                   ModificadoPor = {username}, FechaModificacion = {ahora}
            WHERE  Id = {pf.Id}
            """, ct);

        // Cambiar grupo → FACTURADA
        await ActualizarEstadoGrupo(pf.IdGrupo, pf, "FACTURADA",
            $"CFDI timbrado UUID: {uuid}", ahora, username, userId, ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "[Timbrado] CFDI {UUID} timbrado exitosamente — Grupo: {IdGrupo}", uuid, pf.IdGrupo);

        return new CfdiDto(
            Id:              cfdiId,
            IdPreFactura:    pf.Id,
            UUID:            uuid,
            Version:         pf.Version,
            Total:           pf.Total,
            Estado:          "VIGENTE",
            UrlXml:          urlXml,
            UrlPdf:          urlPdf,
            NoCertificadoSAT: noCert,
            SelloSAT:        selloSat,
            FechaTimbrado:   ahora,
            FechaCreacion:   ahora
        );
    }

    private async Task ProcesarError(
        PreFacturaTimbradoRow pf,
        PacResultadoDto       resultado,
        DateTime              ahora,
        string                username,
        Guid?                 userId,
        CancellationToken     ct)
    {
        var intentos = pf.IntentosTimbrado + 1;
        var error    = resultado.MensajeError;

        await db.Database.ExecuteSqlAsync($"""
            UPDATE fac.PreFacturas
            SET    Estado = 'ERROR_TIMBRADO', IntentosTimbrado = {intentos},
                   UltimoErrorTimbrado = {error},
                   ModificadoPor = {username}, FechaModificacion = {ahora}
            WHERE  Id = {pf.Id}
            """, ct);

        var detalle = $"[{resultado.CodigoError}] {resultado.MensajeError}";
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO aud.LogProcesamiento
                (IdGrupo, Paso, Nivel, Mensaje, FechaEvento)
            VALUES ({pf.IdGrupo}, 'TIMBRADO', 'ERROR', {detalle}, {ahora})
            """, ct);

        await ActualizarEstadoGrupo(pf.IdGrupo, pf, "ERROR_TIMBRADO_MANUAL",
            $"Error timbrado: {error}", ahora, username, userId, ct);

        await db.SaveChangesAsync(ct);
    }

    private async Task ActualizarEstadoGrupo(
        Guid                  idGrupo,
        PreFacturaTimbradoRow pf,
        string                claveDestionacion,
        string                motivo,
        DateTime              ahora,
        string                username,
        Guid?                 userId,
        CancellationToken     ct)
    {
        var estadoInfo = await db.Database
            .SqlQuery<GrupoEstadoRow>($"""
                SELECT g.IdEstadoGrupo AS EstadoActualId,
                       ne.Id           AS EstadoNuevoId
                FROM   rec.GruposReceta g
                CROSS JOIN (SELECT Id FROM cat.EstadosGrupo WHERE Clave = {claveDestionacion}) ne
                WHERE  g.Id = {idGrupo}
                """)
            .FirstOrDefaultAsync(ct);

        if (estadoInfo is null) return;

        await db.Database.ExecuteSqlAsync($"""
            UPDATE rec.GruposReceta
            SET    IdEstadoGrupo = {estadoInfo.EstadoNuevoId},
                   ModificadoPor = {username}, FechaModificacion = {ahora}
            WHERE  Id = {idGrupo}
            """, ct);

        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO aud.HistorialEstadosGrupo
                (IdGrupo, EstadoAnterior, EstadoNuevo, IdUsuario, Motivo, FechaCambio)
            VALUES ({idGrupo}, {estadoInfo.EstadoActualId}, {estadoInfo.EstadoNuevoId},
                    {userId}, {motivo}, {ahora})
            """, ct);
    }

    private record PreFacturaTimbradoRow(
        Guid    Id,
        Guid    IdGrupo,
        string  Estado,
        decimal Total,
        decimal Subtotal,
        decimal TotalIVA,
        string  Version,
        string  TipoComprobante,
        int     IntentosTimbrado,
        string  RFCEmisor,
        string? NombreEmisor,
        string  RFCReceptor,
        string? NombreReceptor);

    private record GrupoEstadoRow(int EstadoActualId, int EstadoNuevoId);
}
