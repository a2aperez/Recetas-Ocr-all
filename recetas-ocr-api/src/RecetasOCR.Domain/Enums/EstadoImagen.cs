namespace RecetasOCR.Domain.Enums;

public enum EstadoImagen
{
    Recibida = 1,
    Legible,
    Ilegible,
    CapturaManualCompleta,
    OcrAprobado,
    OcrBajaConfianza,
    ExtraccionCompleta,
    ExtraccionIncompleta,
    Revisada,
    Rechazada
}
