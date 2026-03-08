using RecetasOCR.Application.DTOs.Facturacion;

namespace RecetasOCR.Application.Common.Interfaces;

/// <summary>
/// Abstracción del proveedor autorizado de certificación (PAC) para emisión de CFDI 4.0.
/// En producción se implementa con el PAC contratado (e.g. Finkok, Edicom).
/// En desarrollo/staging se usa PacServiceStub que simula el timbrado.
/// </summary>
public interface IPacService
{
    Task<PacResultadoDto> TimbrarAsync(string xmlCfdi, CancellationToken ct = default);
    Task<PacResultadoDto> CancelarAsync(string uuid, string motivo, CancellationToken ct = default);
}
