using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Facturacion;

namespace RecetasOCR.Infrastructure.Services;

/// <summary>
/// Stub de IPacService para entornos sin PAC real configurado.
/// Simula un timbrado exitoso con UUID simulado.
/// Sustituir por la implementación real del PAC contratado en producción.
/// </summary>
public class PacServiceStub(ILogger<PacServiceStub> logger) : IPacService
{
    public Task<PacResultadoDto> TimbrarAsync(string xmlCfdi, CancellationToken ct = default)
    {
        var uuid = Guid.NewGuid().ToString().ToUpperInvariant();
        logger.LogWarning("PAC STUB — timbrado simulado UUID: {UUID}", uuid);

        var xmlTimbrado = xmlCfdi.Replace("/>",
            $""" NoCertificadoSAT="20001000000300022323" SelloSAT="STUB_SELLO_SAT" TimbreFiscalDigital="{uuid}"/>""",
            StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(new PacResultadoDto(
            Exitoso:          true,
            UUID:             uuid,
            XmlTimbrado:      xmlTimbrado,
            QrBase64:         null,
            CadenaOriginal:   $"||4.0|{uuid}||",
            NoCertificadoSAT: "20001000000300022323",
            SelloSAT:         "STUB_SELLO_SAT",
            MensajeError:     null,
            CodigoError:      null
        ));
    }

    public Task<PacResultadoDto> CancelarAsync(string uuid, string motivo, CancellationToken ct = default)
    {
        logger.LogWarning("PAC STUB — cancelación simulada UUID: {UUID} | Motivo: {Motivo}", uuid, motivo);

        return Task.FromResult(new PacResultadoDto(
            Exitoso:          true,
            UUID:             uuid,
            XmlTimbrado:      null,
            QrBase64:         null,
            CadenaOriginal:   null,
            NoCertificadoSAT: null,
            SelloSAT:         null,
            MensajeError:     null,
            CodigoError:      null
        ));
    }
}
