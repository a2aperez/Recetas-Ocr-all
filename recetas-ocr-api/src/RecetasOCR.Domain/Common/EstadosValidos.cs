using System.Text.RegularExpressions;
using RecetasOCR.Domain.Enums;

namespace RecetasOCR.Domain.Common;

/// <summary>
/// Define las transiciones de estado válidas del dominio.
/// Usada por los handlers para validar pre-condiciones antes de ejecutar
/// una operación, lanzando EstadoInvalidoException si no se cumple.
///
/// Las reglas reflejan exactamente el flujo de negocio:
///   rec.Imagenes → cat.EstadosImagen
///   rec.GruposReceta → cat.EstadosGrupo
/// </summary>
public static class EstadosValidos
{
    /// <summary>
    /// Estados de imagen desde los cuales se puede asignar a revisión humana.
    /// El revisor recibe imágenes que ya pasaron por OCR o por captura manual.
    /// </summary>
    public static EstadoImagen[] ImagenesPendientesRevision() =>
    [
        EstadoImagen.OcrAprobado,
        EstadoImagen.OcrBajaConfianza,
        EstadoImagen.ExtraccionCompleta,
        EstadoImagen.ExtraccionIncompleta,
        EstadoImagen.CapturaManualCompleta
    ];

    /// <summary>
    /// Estados finales de una imagen. No se puede realizar ninguna
    /// operación adicional sobre imágenes en estos estados.
    /// Corresponden a EsFinal=1 en cat.EstadosImagen.
    /// </summary>
    public static EstadoImagen[] ImagenesFinales() =>
    [
        EstadoImagen.Revisada,
        EstadoImagen.Rechazada
    ];

    /// <summary>
    /// Estados de grupo desde los cuales se puede iniciar la facturación CFDI 4.0.
    /// Regla inamovible: solo REVISADO_COMPLETO avanza a facturación.
    /// Si el grupo no está en este estado → lanzar GrupoNoFacturableException.
    /// </summary>
    public static EstadoGrupo[] GruposFacturables() =>
    [
        EstadoGrupo.RevisadoCompleto
    ];

    /// <summary>
    /// Estados de grupo que indican procesamiento activo o pendiente.
    /// Usados para filtrar grupos que aún no han completado su ciclo OCR.
    /// </summary>
    public static EstadoGrupo[] GruposEnProceso() =>
    [
        EstadoGrupo.Recibido,
        EstadoGrupo.Procesando,
        EstadoGrupo.RequiereCapturaManual,
        EstadoGrupo.GrupoIncompleto
    ];

    /// <summary>
    /// Convierte un EstadoImagen a su clave exacta en cat.EstadosImagen (SCREAMING_SNAKE_CASE).
    /// Ej: OcrAprobado → OCR_APROBADO, ExtraccionCompleta → EXTRACCION_COMPLETA.
    /// </summary>
    public static string ToDbClave(EstadoImagen estado) =>
        Regex.Replace(estado.ToString(), @"(?<=[a-z])([A-Z])", "_$1").ToUpperInvariant();
}
