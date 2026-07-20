using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Infrastructure.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/contact")]
public sealed class ContactController(
    IEmailSender emailSender,
    IOptions<SmtpEmailOptions> smtpOptions) : ControllerBase
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

    private static void ValidateText(string? value, string field, string message, int maxLength, List<ValidationError> errors, int minLength = 1)
    {
        var text = value?.Trim() ?? string.Empty;
        if (text.Length < minLength || text.Length > maxLength || text.Contains('<') || text.Contains('>'))
        {
            errors.Add(new ValidationError(field, message));
        }
    }
}

public sealed record ContactRequest(string? Nombre, string? Email, string? Asunto, string? Mensaje);
