using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Facturacion;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Facturacion;

// ── Command ───────────────────────────────────────────────────────────────────

public record GenerarPreFacturaCommand(Guid IdGrupo) : IRequest<PreFacturaDto>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class GenerarPreFacturaCommandValidator : AbstractValidator<GenerarPreFacturaCommand>
{
    public GenerarPreFacturaCommandValidator()
    {
        RuleFor(x => x.IdGrupo).NotEmpty();
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class GenerarPreFacturaCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser,
    ILogger<GenerarPreFacturaCommandHandler> logger)
    : IRequestHandler<GenerarPreFacturaCommand, PreFacturaDto>
{
    private const decimal PrecioUnitarioPorDefecto = 100m;
    private const decimal IvaTasa                 = 0.16m;

    public async Task<PreFacturaDto> Handle(
        GenerarPreFacturaCommand command,
        CancellationToken        ct)
    {
        var ahora    = DateTime.UtcNow;
        var username = currentUser.Username ?? "sistema";
        var userId   = currentUser.UserId;

        // ── Cargar grupo + estado ─────────────────────────────────────────────
        var grupo = await db.Database
            .SqlQuery<GrupoFacRow>($"""
                SELECT g.Id, g.IdCliente, g.IdAseguradora, g.IdEstadoGrupo,
                       g.NombrePaciente, g.FechaConsulta,
                       eg.Clave AS EstadoClave
                FROM   rec.GruposReceta g
                INNER JOIN cat.EstadosGrupo eg ON eg.Id = g.IdEstadoGrupo
                WHERE  g.Id = {command.IdGrupo}
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntidadNoEncontradaException("GrupoReceta", command.IdGrupo);

        if (grupo.EstadoClave != "REVISADO_COMPLETO")
            throw new GrupoNoFacturableException(grupo.Id, grupo.EstadoClave);

        // ── Verificar que no exista prefactura activa ─────────────────────────
        var prefacActiva = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value FROM fac.PreFacturas
                WHERE  IdGrupo = {command.IdGrupo}
                  AND  Estado NOT IN ('CANCELADA', 'ERROR')
                """)
            .FirstAsync(ct);

        if (prefacActiva > 0)
            throw new EstadoInvalidoException(
                "PreFactura", "ACTIVA",
                ["Ya existe una PreFactura activa para este grupo."]);

        // ── Receptor para datos fiscales ──────────────────────────────────────
        var receptor = grupo.IdCliente.HasValue
            ? await db.Database
                .SqlQuery<ReceptorFacRow>($"""
                    SELECT TOP 1 Id, RFC, NombreRazonSocial
                    FROM   fac.Receptores
                    WHERE  IdCliente = {grupo.IdCliente}
                    """)
                .FirstOrDefaultAsync(ct)
            : null;

        // ── Preferencias fiscales (UsoCFDI, MetodoPago, FormaPago) ────────────
        var paramKey = $"FISCAL_{command.IdGrupo:N}";
        var paramRow = await db.Database
            .SqlQuery<ParametroValorRow>($"""
                SELECT TOP 1 Valor FROM cfg.Parametros WHERE Clave = {paramKey}
                """)
            .FirstOrDefaultAsync(ct);

        FiscalConfig? cfg = paramRow is not null
            ? JsonSerializer.Deserialize<FiscalConfig>(paramRow.Valor)
            : null;

        // ── Validar completitud de datos fiscales ─────────────────────────────
        var faltantes = new List<string>();
        if (receptor is null || string.IsNullOrWhiteSpace(receptor.RFC))
            faltantes.Add("RFC");
        if (receptor is null || string.IsNullOrWhiteSpace(receptor.NombreRazonSocial))
            faltantes.Add("NombreFiscal");
        if (cfg is null || cfg.UsoCfdiId == 0)  faltantes.Add("UsoCFDI");
        if (cfg is null || cfg.MetodoPagoId == 0) faltantes.Add("MetodoPago");
        if (cfg is null || cfg.FormaPagoId == 0)  faltantes.Add("FormaPago");

        if (faltantes.Count > 0)
        {
            // Cambiar grupo a DATOS_FISCALES_INCOMPLETOS
            var estadoInc = await db.Database
                .SqlQuery<EstadoIdRow>($"""
                    SELECT Id FROM cat.EstadosGrupo WHERE Clave = 'DATOS_FISCALES_INCOMPLETOS'
                    """)
                .FirstAsync(ct);

            await db.Database.ExecuteSqlAsync($"""
                UPDATE rec.GruposReceta
                SET    IdEstadoGrupo = {estadoInc.Id},
                       ModificadoPor = {username}, FechaModificacion = {ahora}
                WHERE  Id = {command.IdGrupo}
                """, ct);

            var motivo = "Datos fiscales incompletos: " + string.Join(", ", faltantes);
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO aud.HistorialEstadosGrupo
                    (IdGrupo, EstadoAnterior, EstadoNuevo, IdUsuario, Motivo, FechaCambio)
                VALUES ({command.IdGrupo}, {grupo.IdEstadoGrupo}, {estadoInc.Id},
                        {userId}, {motivo}, {ahora})
                """, ct);

            var failures = faltantes
                .Select(f => new FluentValidation.Results.ValidationFailure(
                    f, $"El campo '{f}' es obligatorio para facturar."))
                .ToList();
            throw new ValidationException(failures);
        }

        // ── Emisor de la aseguradora ──────────────────────────────────────────
        var emisor = await db.Database
            .SqlQuery<EmisorFacRow>($"""
                SELECT TOP 1 Id, RFC AS RFCEmisor, RazonSocial
                FROM   fac.Emisores
                WHERE  IdAseguradora = {grupo.IdAseguradora} AND Activo = 1
                ORDER  BY Id
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntidadNoEncontradaException("Emisor", grupo.IdAseguradora);

        // ── Moneda MXN ────────────────────────────────────────────────────────
        var monedaId = await db.Database
            .SqlQuery<int>($"""
                SELECT TOP 1 Id FROM cat.Monedas WHERE Clave = 'MXN' AND Activo = 1
                """)
            .FirstAsync(ct);

        // ── Medicamentos del grupo con ClaveSAT ───────────────────────────────
        var meds = await db.Database
            .SqlQuery<MedicamentoFacRow>($"""
                SELECT mr.Id, mr.NumeroPrescripcion, mr.NombreComercial,
                       mr.Presentacion, mr.CantidadNumero, mr.CodigoEAN,
                       ISNULL(m.ClaveSAT, '51101500')      AS ClaveProdServ,
                       ISNULL(m.ClaveUnidadSAT, 'H87')     AS ClaveUnidad,
                       CAST(ISNULL(m.IVATasa, 0.16) AS DECIMAL(5,4)) AS IvaTasaCat
                FROM   med.MedicamentosReceta mr
                LEFT   JOIN cat.Medicamentos m ON m.Id = mr.IdMedicamentoCatalogo
                WHERE  mr.IdGrupo = {command.IdGrupo}
                """)
            .ToListAsync(ct);

        // ── Calcular totales ──────────────────────────────────────────────────
        decimal subtotal = 0;
        decimal totalIva = 0;

        var lineas = meds.Select((m, i) =>
        {
            decimal cantidad   = m.CantidadNumero ?? 1m;
            decimal valor      = PrecioUnitarioPorDefecto;
            decimal importe    = Math.Round(cantidad * valor, 2);
            decimal ivaImporte = Math.Round(importe * IvaTasa, 2);
            string  desc       = string.Join(" ",
                new[] { m.NombreComercial, m.Presentacion }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            if (string.IsNullOrWhiteSpace(desc)) desc = "Medicamento";

            subtotal += importe;
            totalIva += ivaImporte;

            return (
                PartidaId:    Guid.NewGuid(),
                NumeroLinea:  i + 1,
                Med:          m,
                Descripcion:  desc,
                Cantidad:     cantidad,
                Valor:        valor,
                Importe:      importe,
                IvaImporte:   ivaImporte
            );
        }).ToList();

        // ── INSERT fac.PreFacturas ─────────────────────────────────────────────
        var prefacId = Guid.NewGuid();
        decimal total = Math.Round(subtotal + totalIva, 2);

        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO fac.PreFacturas
                (Id, IdGrupo, IdEmisor, IdReceptor, TipoComprobante, Version,
                 MetodoPagoId, FormaPagoId, MonedaId, UsoCFDIId, TipoCambio,
                 Exportacion, Subtotal, Descuento, TotalIVA, TotalIEPS, Total,
                 Estado, FechaGeneracion, IntentosTimbrado,
                 ModificadoPor, FechaModificacion)
            VALUES
                ({prefacId}, {command.IdGrupo}, {emisor.Id}, {receptor!.Id}, 'I', '4.0',
                 {cfg!.MetodoPagoId}, {cfg.FormaPagoId}, {monedaId}, {cfg.UsoCfdiId}, 1.0000,
                 '01', {subtotal}, 0, {totalIva}, 0, {total},
                 'BORRADOR', {ahora}, 0, {username}, {ahora})
            """, ct);

        // ── INSERT fac.PartidasPreFactura ─────────────────────────────────────
        foreach (var ln in lineas)
        {
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO fac.PartidasPreFactura
                    (Id, IdPreFactura, IdMedicamentoReceta, NumeroLinea,
                     ClaveProdServ, ClaveUnidad, NoIdentificacion, Descripcion,
                     Cantidad, ValorUnitario, Descuento, Importe,
                     ObjetoImpuesto, IVATasa, IVAImporte, IEPSTasa, IEPSImporte,
                     ModificadoPor, FechaModificacion)
                VALUES
                    ({ln.PartidaId}, {prefacId}, {ln.Med.Id}, {ln.NumeroLinea},
                     {ln.Med.ClaveProdServ}, {ln.Med.ClaveUnidad}, {ln.Med.CodigoEAN},
                     {ln.Descripcion}, {ln.Cantidad}, {ln.Valor}, 0, {ln.Importe},
                     '02', {IvaTasa}, {ln.IvaImporte}, 0, 0,
                     {username}, {ahora})
                """, ct);
        }

        // ── Actualizar estado grupo → PREFACTURA_GENERADA ─────────────────────
        var estadoPrefac = await db.Database
            .SqlQuery<EstadoIdRow>($"""
                SELECT Id FROM cat.EstadosGrupo WHERE Clave = 'PREFACTURA_GENERADA'
                """)
            .FirstAsync(ct);

        await db.Database.ExecuteSqlAsync($"""
            UPDATE rec.GruposReceta
            SET    IdEstadoGrupo = {estadoPrefac.Id},
                   ModificadoPor = {username}, FechaModificacion = {ahora}
            WHERE  Id = {command.IdGrupo}
            """, ct);

        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO aud.HistorialEstadosGrupo
                (IdGrupo, EstadoAnterior, EstadoNuevo, IdUsuario, Motivo, FechaCambio)
            VALUES ({command.IdGrupo}, {grupo.IdEstadoGrupo}, {estadoPrefac.Id},
                    {userId}, 'Pre-factura generada', {ahora})
            """, ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "[Facturacion] PreFactura {Id} generada para grupo {IdGrupo}", prefacId, command.IdGrupo);

        // ── Mapear conceptos para respuesta ───────────────────────────────────
        var conceptos = lineas.Select(ln => new ConceptoFacturaDto(
            Id:                 ln.PartidaId,
            NumeroPrescripcion: ln.Med.NumeroPrescripcion,
            Descripcion:        ln.Descripcion,
            ClaveSAT:           ln.Med.ClaveProdServ,
            Cantidad:           ln.Cantidad,
            PrecioUnitario:     ln.Valor,
            Importe:            ln.Importe,
            IVA:                ln.IvaImporte
        )).ToList();

        return new PreFacturaDto(
            Id:                prefacId,
            IdGrupo:           command.IdGrupo,
            Estado:            "BORRADOR",
            RFC:               receptor.RFC,
            NombreFiscal:      receptor.NombreRazonSocial,
            UsoCFDI:           cfg.UsoCfdiClave,
            MetodoPago:        cfg.MetodoPagoClave,
            FormaPago:         cfg.FormaPagoClave,
            Subtotal:          subtotal,
            IVA:               totalIva,
            Total:             total,
            Conceptos:         conceptos,
            FechaCreacion:     ahora,
            FechaModificacion: ahora
        );
    }

    // ── Local SQL result rows ─────────────────────────────────────────────────

    private record GrupoFacRow(
        Guid     Id,
        Guid?    IdCliente,
        int      IdAseguradora,
        int      IdEstadoGrupo,
        string?  NombrePaciente,
        DateOnly? FechaConsulta,
        string   EstadoClave);

    private record ReceptorFacRow(Guid Id, string RFC, string NombreRazonSocial);
    private record ParametroValorRow(string Valor);
    private record EmisorFacRow(int Id, string RFCEmisor, string RazonSocial);

    private record MedicamentoFacRow(
        Guid    Id,
        int     NumeroPrescripcion,
        string? NombreComercial,
        string? Presentacion,
        int?    CantidadNumero,
        string? CodigoEAN,
        string  ClaveProdServ,
        string  ClaveUnidad,
        decimal IvaTasaCat);
}

// ── Helpers ───────────────────────────────────────────────────────────────────

internal sealed class EstadoIdRow(int Id)
{
    public int Id { get; } = Id;
}

internal sealed class FiscalConfig
{
    public int    UsoCfdiId       { get; set; }
    public string UsoCfdiClave    { get; set; } = string.Empty;
    public int    MetodoPagoId    { get; set; }
    public string MetodoPagoClave { get; set; } = string.Empty;
    public int    FormaPagoId     { get; set; }
    public string FormaPagoClave  { get; set; } = string.Empty;
}
