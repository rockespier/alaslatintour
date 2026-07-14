using AlasApp.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace AlasApp.Infrastructure.Email;

public sealed class SmtpEmailSender(
    IOptionsMonitor<SmtpEmailOptions> optionsMonitor,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message.To);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Subject);

        var options = optionsMonitor.CurrentValue;
        if (!options.Enabled)
        {
            logger.LogInformation("Envio de correo omitido porque SmtpEmail:Enabled=false. Destinatario: {Recipient}.", message.To);
            return;
        }

        Validate(options);

        using var smtpClient = new SmtpClient(options.Host.Trim(), options.Port)
        {
            EnableSsl = options.EnableSsl,
            Credentials = new NetworkCredential(options.Username.Trim(), options.Password)
        };

        var fromAddress = string.IsNullOrWhiteSpace(options.FromName)
            ? new MailAddress(options.FromEmail.Trim())
            : new MailAddress(options.FromEmail.Trim(), options.FromName.Trim());

        using var mailMessage = new MailMessage
        {
            From = fromAddress,
            Subject = message.Subject.Trim(),
            Body = string.IsNullOrWhiteSpace(message.HtmlBody) ? message.TextBody : message.HtmlBody,
            IsBodyHtml = !string.IsNullOrWhiteSpace(message.HtmlBody)
        };
        mailMessage.To.Add(message.To.Trim());

        logger.LogInformation("Enviando correo via SMTP a {Recipient} con asunto {Subject}.", message.To, message.Subject);

        try
        {
            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            logger.LogInformation("Correo enviado via SMTP a {Recipient}.", message.To);
        }
        catch (SmtpException exception)
        {
            logger.LogWarning(
                exception,
                "SMTP rechazo el envio a {Recipient}. StatusCode: {StatusCode}.",
                message.To,
                exception.StatusCode);
            throw;
        }
    }

    private static void Validate(SmtpEmailOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host))
        {
            throw new InvalidOperationException("SmtpEmail:Host es obligatorio cuando SmtpEmail:Enabled=true.");
        }

        if (options.Port <= 0)
        {
            throw new InvalidOperationException("SmtpEmail:Port debe ser mayor que cero cuando SmtpEmail:Enabled=true.");
        }

        if (string.IsNullOrWhiteSpace(options.Username))
        {
            throw new InvalidOperationException("SmtpEmail:Username es obligatorio cuando SmtpEmail:Enabled=true.");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException("SmtpEmail:Password es obligatorio cuando SmtpEmail:Enabled=true.");
        }

        if (string.IsNullOrWhiteSpace(options.FromEmail))
        {
            throw new InvalidOperationException("SmtpEmail:FromEmail es obligatorio cuando SmtpEmail:Enabled=true.");
        }
    }
}
