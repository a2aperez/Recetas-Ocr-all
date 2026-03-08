namespace RecetasOCR.Domain.Enums;

/// <summary>
/// Nivel de severidad para registros en aud.LogProcesamiento.
/// Columna Nivel NVARCHAR(10).
/// </summary>
public enum NivelLog
{
    Info = 1,
    Warn,
    Error
}
