using AlasApp.Application.Abstractions.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

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

        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(string.IsNullOrWhiteSpace(options.FromName)
            ? new MailboxAddress(options.FromEmail.Trim(), options.FromEmail.Trim())
            : new MailboxAddress(options.FromName.Trim(), options.FromEmail.Trim()));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To.Trim()));
        mimeMessage.Subject = message.Subject.Trim();

        var bodyBuilder = new BodyBuilder();
        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            bodyBuilder.HtmlBody = message.HtmlBody;
            bodyBuilder.TextBody = message.TextBody;
        }
        else
        {
            bodyBuilder.TextBody = message.TextBody;
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        logger.LogInformation("Enviando correo via SMTP a {Recipient} con asunto {Subject}.", message.To, message.Subject);

        using var smtpClient = new SmtpClient();
        try
        {
            var secureSocketOptions = options.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await smtpClient.ConnectAsync(options.Host.Trim(), options.Port, secureSocketOptions, cancellationToken);
            await smtpClient.AuthenticateAsync(options.Username.Trim(), options.Password, cancellationToken);
            await smtpClient.SendAsync(mimeMessage, cancellationToken);
            await smtpClient.DisconnectAsync(true, cancellationToken);
            logger.LogInformation("Correo enviado via SMTP a {Recipient}.", message.To);
        }
        catch (Exception exception) when (exception is SmtpCommandException or SmtpProtocolException or AuthenticationException)
        {
            logger.LogWarning(exception, "SMTP rechazo el envio a {Recipient}.", message.To);
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
