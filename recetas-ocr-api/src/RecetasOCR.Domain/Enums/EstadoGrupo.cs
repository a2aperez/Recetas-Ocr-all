namespace RecetasOCR.Domain.Enums;

public enum EstadoGrupo
{
    Recibido = 1,
    RequiereCapturaManual,
    Procesando,
    GrupoIncompleto,
    RevisionPendiente,
    RevisadoCompleto,
    DatosFiscalesIncompletos,
    PendienteAutorizacion,
    PendienteFacturacion,
    PrefacturaGenerada,
    Facturada,
    ErrorTimbradoManual,
    Rechazado
}
