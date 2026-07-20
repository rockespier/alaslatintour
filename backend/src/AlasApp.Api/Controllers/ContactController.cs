using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Api.Authentication;
using AlasApp.Infrastructure.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/contact")]
public sealed class ContactController(
    IEmailSender emailSender,
    IOptions<SmtpEmailOptions> smtpOptions,
    IOptions<ContactCaptchaOptions> captchaOptions,
    IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Send([FromBody] ContactRequest request, CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validación.", errors);
        }

        await EnsureCaptchaIsValidAsync(request.TurnstileToken, captchaOptions.Value, cancellationToken);

        var recipient = smtpOptions.Value.FromEmail;
        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new InvalidOperationException("No está configurado el destinatario del formulario de contacto.");
        }

        var nombre = request.Nombre?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var asunto = request.Asunto?.Trim() ?? string.Empty;
        var mensaje = request.Mensaje?.Trim() ?? string.Empty;
        var subject = $"Contacto web: {asunto}";
        var textBody = $"Nombre: {nombre}\nCorreo: {email}\n\nMensaje:\n{mensaje}";
        var htmlBody = $"<p><strong>Nombre:</strong> {WebUtility.HtmlEncode(nombre)}</p>" +
            $"<p><strong>Correo:</strong> {WebUtility.HtmlEncode(email)}</p>" +
            $"<p><strong>Mensaje:</strong><br>{WebUtility.HtmlEncode(mensaje).Replace("\n", "<br>")}</p>";

        await emailSender.SendAsync(new EmailMessage(recipient, subject, textBody, htmlBody), cancellationToken);
        return Ok(new { message = "Mensaje enviado correctamente." });
    }

    private static List<ValidationError> Validate(ContactRequest request)
    {
        var errors = new List<ValidationError>();
        ValidateText(request.Nombre, "nombre", "El nombre es obligatorio.", 120, errors);
        ValidateText(request.Asunto, "asunto", "El asunto es obligatorio.", 160, errors);
        ValidateText(request.Mensaje, "mensaje", "El mensaje debe tener entre 10 y 4.000 caracteres.", 4000, errors, 10);

        try
        {
            _ = new MailAddress(request.Email?.Trim() ?? string.Empty);
        }
        catch (FormatException)
        {
            errors.Add(new ValidationError("email", "El correo electrónico no es válido."));
        }

        return errors;
    }

    private async Task EnsureCaptchaIsValidAsync(string? token, ContactCaptchaOptions options, CancellationToken cancellationToken)
    {
        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            throw new InvalidOperationException("ContactCaptcha:SecretKey es obligatorio cuando ContactCaptcha:Enabled=true.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ValidationException("La verificación anti-spam es obligatoria.",
                [new ValidationError("turnstileToken", "Completa la verificación anti-spam.")]);
        }

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"] = options.SecretKey,
            ["response"] = token,
            ["remoteip"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
        });
        using var response = await httpClientFactory.CreateClient().PostAsync(
            "https://challenges.cloudflare.com/turnstile/v0/siteverify", content, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<TurnstileValidationResult>(cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode || result?.Success != true)
        {
            throw new ValidationException("La verificación anti-spam no fue válida. Inténtalo de nuevo.",
                [new ValidationError("turnstileToken", "Completa nuevamente la verificación anti-spam.")]);
        }
    }

    private static void ValidateText(string? value, string field, string message, int maxLength, List<ValidationError> errors, int minLength = 1)
    {
        var text = value?.Trim() ?? string.Empty;
        if (text.Length < minLength || text.Length > maxLength || text.Contains('<') || text.Contains('>'))
        {
            errors.Add(new ValidationError(field, message));
        }
    }
}

public sealed record ContactRequest(string? Nombre, string? Email, string? Asunto, string? Mensaje, string? TurnstileToken);

public sealed record TurnstileValidationResult(bool Success);
