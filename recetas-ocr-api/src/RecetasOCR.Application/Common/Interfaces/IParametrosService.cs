namespace RecetasOCR.Application.Common.Interfaces;

/// <summary>
/// Servicio de acceso a cfg.Parametros con caché en memoria de 5 minutos.
/// Implementado en Infrastructure.Services.ParametrosService.
/// Nunca expone parámetros con EsSecreto=1 fuera de la capa Infrastructure.
/// Las claves disponibles están en RecetasOCR.Domain.Common.Constantes.Parametros.
/// </summary>
public interface IParametrosService
{
    /// <summary>
    /// Obtiene el valor string de un parámetro por su clave.
    /// Retorna null si la clave no existe o el parámetro está inactivo.
    /// </summary>
    /// <param name="clave">Clave exacta (ej: "OCR_CONFIANZA_MINIMA").</param>
    Task<string?> ObtenerAsync(string clave, CancellationToken ct = default);

    /// <summary>
    /// Obtiene el valor de un parámetro parseado como decimal.
    /// Retorna <paramref name="defaultValue"/> si la clave no existe o no es decimal.
    /// </summary>
    Task<decimal> ObtenerDecimalAsync(string clave, decimal defaultValue, CancellationToken ct = default);

    /// <summary>
    /// Obtiene el valor de un parámetro parseado como entero.
    /// Retorna <paramref name="defaultValue"/> si la clave no existe o no es entero.
    /// </summary>
    Task<int> ObtenerIntAsync(string clave, int defaultValue, CancellationToken ct = default);

    /// <summary>
    /// Invalida el caché de un parámetro específico.
    /// Llamar desde el handler de actualización de cfg.Parametros.
    /// </summary>
    void InvalidarCache(string clave);

    /// <summary>
    /// Invalida todo el caché de parámetros.
    /// Llamar tras una importación masiva de configuración.
    /// </summary>
    void InvalidarCacheTotal();
}
