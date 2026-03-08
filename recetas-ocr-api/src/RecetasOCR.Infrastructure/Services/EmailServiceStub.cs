using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;

namespace RecetasOCR.Infrastructure.Services;

/// <summary>
/// Stub de IEmailService para entornos sin configuración SMTP.
/// Registra el intento como Information y retorna true.
/// Sustituir por SmtpEmailService o SendGridEmailService en producción.
/// </summary>
public class EmailServiceStub : IEmailService
{
    private readonly ILogger<EmailServiceStub> _logger;

    public EmailServiceStub(ILogger<EmailServiceStub> logger) => _logger = logger;

    public Task<bool> EnviarAsync(
        string para, string asunto, string cuerpoHtml, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[EmailStub] Para: {Para} | Asunto: {Asunto}", para, asunto);
        return Task.FromResult(true);
    }

    public Task<int> EnviarMultipleAsync(
        IEnumerable<string> destinatarios, string asunto, string cuerpoHtml,
        CancellationToken ct = default)
    {
        var lista = destinatarios.ToList();
        _logger.LogInformation(
            "[EmailStub] {Count} destinatarios | Asunto: {Asunto}", lista.Count, asunto);
        return Task.FromResult(lista.Count);
    }
}
