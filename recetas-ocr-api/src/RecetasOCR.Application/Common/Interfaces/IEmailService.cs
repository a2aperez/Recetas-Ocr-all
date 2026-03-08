namespace RecetasOCR.Application.Common.Interfaces;

/// <summary>
/// Servicio de envío de correos para notificaciones del sistema.
/// Implementado en Infrastructure.Services.EmailService.
/// Casos de uso principales:
///   - Notificar asignación de revisión al REVISOR
///   - Notificar CFDI timbrado al FACTURISTA y al cliente
///   - Notificar error de timbrado al FACTURISTA
///   - Notificar bloqueo de cuenta al usuario
///   - Notificar cambio de contraseña
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envía un correo electrónico en formato HTML a un destinatario.
    /// </summary>
    /// <param name="para">Dirección de correo destino.</param>
    /// <param name="asunto">Asunto del correo.</param>
    /// <param name="cuerpoHtml">Cuerpo en formato HTML.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>True si el envío fue aceptado por el servidor SMTP/API.</returns>
    Task<bool> EnviarAsync(
        string para,
        string asunto,
        string cuerpoHtml,
        CancellationToken ct = default);

    /// <summary>
    /// Envía el mismo correo a múltiples destinatarios.
    /// Los fallos individuales no interrumpen el envío a los demás.
    /// </summary>
    /// <param name="destinatarios">Lista de direcciones destino.</param>
    /// <param name="asunto">Asunto del correo.</param>
    /// <param name="cuerpoHtml">Cuerpo en formato HTML.</param>
    /// <returns>Número de envíos exitosos.</returns>
    Task<int> EnviarMultipleAsync(
        IEnumerable<string> destinatarios,
        string asunto,
        string cuerpoHtml,
        CancellationToken ct = default);
}
