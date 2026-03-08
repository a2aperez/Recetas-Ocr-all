using System.Text.Json;
using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Facturacion;
using RecetasOCR.Domain.Exceptions;

namespace RecetasOCR.Application.Features.Facturacion;

// ── Command ───────────────────────────────────────────────────────────────────

public record ActualizarDatosFiscalesCommand(
    Guid    IdGrupo,
    string  RFC,
    string  NombreFiscal,
    string  UsoCFDI,
    string  MetodoPago,
    string  FormaPago,
    string? RegimenFiscal
) : IRequest<bool>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class ActualizarDatosFiscalesCommandValidator : AbstractValidator<ActualizarDatosFiscalesCommand>
{
    private static readonly Regex RfcRegex =
        new(@"^[A-ZÑ&]{3,4}[0-9]{6}[A-Z0-9]{3}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ActualizarDatosFiscalesCommandValidator()
    {
        RuleFor(x => x.IdGrupo).NotEmpty();
        RuleFor(x => x.RFC)
            .NotEmpty()
            .Must(r => RfcRegex.IsMatch(r))
            .WithMessage("RFC inválido. Formato: ^[A-ZÑ&]{3,4}[0-9]{6}[A-Z0-9]{3}$");
        RuleFor(x => x.NombreFiscal).NotEmpty().MaximumLength(300);
        RuleFor(x => x.UsoCFDI).NotEmpty().MaximumLength(10);
        RuleFor(x => x.MetodoPago).NotEmpty().MaximumLength(5);
        RuleFor(x => x.FormaPago).NotEmpty().MaximumLength(5);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class ActualizarDatosFiscalesCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser)
    : IRequestHandler<ActualizarDatosFiscalesCommand, bool>
{
    public async Task<bool> Handle(
        ActualizarDatosFiscalesCommand command,
        CancellationToken              ct)
    {
        var ahora    = DateTime.UtcNow;
        var username = currentUser.Username ?? "sistema";
        var userId   = currentUser.UserId;
        var rfc      = command.RFC.ToUpperInvariant();

        // ── Cargar grupo ──────────────────────────────────────────────────────
        var grupo = await db.Database
            .SqlQuery<GrupoEstadoFiscalRow>($"""
                SELECT g.Id, g.IdCliente, g.IdEstadoGrupo, eg.Clave AS EstadoClave
                FROM   rec.GruposReceta g
                INNER JOIN cat.EstadosGrupo eg ON eg.Id = g.IdEstadoGrupo
                WHERE  g.Id = {command.IdGrupo}
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntidadNoEncontradaException("GrupoReceta", command.IdGrupo);

        // ── Validar catálogos fiscales ────────────────────────────────────────
        var usoCfdi = await db.Database
            .SqlQuery<FiscalCatRow>($"""
                SELECT TOP 1 Id, Clave FROM cat.UsoCFDI
                WHERE Clave = {command.UsoCFDI} AND Activo = 1
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntidadNoEncontradaException("UsoCFDI", command.UsoCFDI);

        var metodoPago = await db.Database
            .SqlQuery<FiscalCatRow>($"""
                SELECT TOP 1 Id, Clave FROM cat.MetodosPago WHERE Clave = {command.MetodoPago}
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntidadNoEncontradaException("MetodoPago", command.MetodoPago);

        var formaPago = await db.Database
            .SqlQuery<FiscalCatRow>($"""
                SELECT TOP 1 Id, Clave FROM cat.FormasPago
                WHERE Clave = {command.FormaPago} AND Activo = 1
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntidadNoEncontradaException("FormaPago", command.FormaPago);

        // ── Resolver RegimenFiscal ────────────────────────────────────────────
        int regimenId;
        if (!string.IsNullOrWhiteSpace(command.RegimenFiscal))
        {
            var reg = await db.Database
                .SqlQuery<EstadoIdRow>($"""
                    SELECT TOP 1 Id FROM cat.RegimenFiscal
                    WHERE Clave = {command.RegimenFiscal} AND Activo = 1
                    """)
                .FirstOrDefaultAsync(ct);
            regimenId = reg?.Id ?? await db.Database
                .SqlQuery<int>($"SELECT TOP 1 Id FROM cat.RegimenFiscal WHERE Activo = 1 ORDER BY Id")
                .FirstAsync(ct);
        }
        else
        {
            regimenId = await db.Database
                .SqlQuery<int>($"SELECT TOP 1 Id FROM cat.RegimenFiscal WHERE Activo = 1 ORDER BY Id")
                .FirstAsync(ct);
        }

        // ── UPSERT fac.Receptores ─────────────────────────────────────────────
        if (grupo.IdCliente.HasValue)
        {
            var receptorExiste = await db.Database
                .SqlQuery<Guid?>($"""
                    SELECT TOP 1 CAST(Id AS UNIQUEIDENTIFIER) FROM fac.Receptores
                    WHERE IdCliente = {grupo.IdCliente}
                    """)
                .FirstOrDefaultAsync(ct);

            if (receptorExiste is null)
            {
                var newReceptorId = Guid.NewGuid();
                await db.Database.ExecuteSqlAsync($"""
                    INSERT INTO fac.Receptores
                        (Id, IdCliente, RFC, NombreRazonSocial, RegimenFiscalId,
                         CodigoPostal, Activo, FechaAlta, ModificadoPor, FechaModificacion)
                    VALUES
                        ({newReceptorId}, {grupo.IdCliente}, {rfc}, {command.NombreFiscal},
                         {regimenId}, '06600', 1, {ahora}, {username}, {ahora})
                    """, ct);
            }
            else
            {
                await db.Database.ExecuteSqlAsync($"""
                    UPDATE fac.Receptores
                    SET    RFC = {rfc},
                           NombreRazonSocial = {command.NombreFiscal},
                           RegimenFiscalId   = {regimenId},
                           ModificadoPor     = {username},
                           FechaModificacion = {ahora}
                    WHERE  IdCliente = {grupo.IdCliente}
                    """, ct);
            }
        }

        // ── UPSERT cfg.Parametros con preferencias fiscales ───────────────────
        var paramKey = $"FISCAL_{command.IdGrupo:N}";
        var fiscalCfg = new FiscalConfig
        {
            UsoCfdiId      = usoCfdi.Id,
            UsoCfdiClave   = usoCfdi.Clave,
            MetodoPagoId   = metodoPago.Id,
            MetodoPagoClave = metodoPago.Clave,
            FormaPagoId    = formaPago.Id,
            FormaPagoClave = formaPago.Clave
        };
        var json = JsonSerializer.Serialize(fiscalCfg);

        var paramExiste = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value FROM cfg.Parametros WHERE Clave = {paramKey}
                """)
            .FirstAsync(ct);

        var paramDesc = $"Config fiscal grupo {command.IdGrupo}";
        if (paramExiste == 0)
        {
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO cfg.Parametros
                    (Clave, Valor, Descripcion, Tipo, EsSecreto,
                     FechaAlta, FechaActualizacion, ModificadoPor, FechaModificacion)
                VALUES
                    ({paramKey}, {json}, {paramDesc},
                     'JSON', 0, {ahora}, {ahora}, {username}, {ahora})
                """, ct);
        }
        else
        {
            await db.Database.ExecuteSqlAsync($"""
                UPDATE cfg.Parametros
                SET    Valor = {json}, FechaActualizacion = {ahora},
                       ModificadoPor = {username}, FechaModificacion = {ahora}
                WHERE  Clave = {paramKey}
                """, ct);
        }

        // ── Si grupo estaba DATOS_FISCALES_INCOMPLETOS → volver a REVISADO_COMPLETO
        if (grupo.EstadoClave == "DATOS_FISCALES_INCOMPLETOS")
        {
            var estadoRevisado = await db.Database
                .SqlQuery<EstadoIdRow>($"""
                    SELECT Id FROM cat.EstadosGrupo WHERE Clave = 'REVISADO_COMPLETO'
                    """)
                .FirstAsync(ct);

            await db.Database.ExecuteSqlAsync($"""
                UPDATE rec.GruposReceta
                SET    IdEstadoGrupo = {estadoRevisado.Id},
                       ModificadoPor = {username}, FechaModificacion = {ahora}
                WHERE  Id = {command.IdGrupo}
                """, ct);

            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO aud.HistorialEstadosGrupo
                    (IdGrupo, EstadoAnterior, EstadoNuevo, IdUsuario, Motivo, FechaCambio)
                VALUES ({command.IdGrupo}, {grupo.IdEstadoGrupo}, {estadoRevisado.Id},
                        {userId}, 'Datos fiscales actualizados', {ahora})
                """, ct);
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    private record GrupoEstadoFiscalRow(Guid Id, Guid? IdCliente, int IdEstadoGrupo, string EstadoClave);
    private record FiscalCatRow(int Id, string Clave);
}
