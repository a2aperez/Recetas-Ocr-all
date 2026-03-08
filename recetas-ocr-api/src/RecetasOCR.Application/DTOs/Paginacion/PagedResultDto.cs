namespace RecetasOCR.Application.DTOs.Paginacion;

/// <summary>
/// Envelope genérico para resultados paginados.
/// Usado en todos los endpoints de listado del sistema.
/// TotalPages se calcula automáticamente.
/// Alineado con la respuesta esperada en el frontend (gruposRecetaApi.getAll()).
/// </summary>
public record PagedResultDto<T>(
    List<T> Items,
    int     Total,
    int     Page,
    int     PageSize
)
{
    /// <summary>
    /// Total de páginas. Mínimo 1 aunque Items esté vacío.
    /// </summary>
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)Total / PageSize)
        : 1;

    /// <summary>
    /// Resultado vacío conservando los parámetros de paginación.
    /// </summary>
    public static PagedResultDto<T> Empty(int page, int pageSize) =>
        new([], 0, page, pageSize);
}
