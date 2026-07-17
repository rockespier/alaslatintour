using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminSettings;
using AlasApp.Application.Common;
using AlasApp.Application.Emails;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Payments.Commands.ApproveBeachToken;

public sealed class ApproveBeachTokenCommandHandler(
    IBeachTokenRepository beachTokenRepository,
    IAdminSettingsRepository adminSettingsRepository,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<ApproveBeachTokenCommand, BeachTokenAdminDto>
{
    public async Task<BeachTokenAdminDto> Handle(ApproveBeachTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await beachTokenRepository.GetEntityByIdAsync(request.TokenId, cancellationToken)
            ?? throw new NotFoundException("Solicitud de token no encontrada.");

        try
        {
            if (token.ExpirationAt.HasValue && token.ExpirationAt.Value <= clock.UtcNow)
            {
                token.MarkExpired();
            }

            token.Approve(GenerateTokenCode(), clock.UtcNow, clock.UtcNow.AddHours(24));
            token.SetUpdated(clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var approvedToken = await beachTokenRepository.GetAdminByIdAsync(token.Id, clock.UtcNow, cancellationToken)
                ?? throw new NotFoundException("Token no encontrado despues de aprobarlo.");

            await SendApprovalEmailAsync(approvedToken, cancellationToken);

            return approvedToken;
        }
        catch (DomainRuleException exception)
        {
            throw new ConflictException(exception.Message);
        }
    }

    private static string GenerateTokenCode()
    {
        var raw = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"{raw[..4]}-{raw[4..]}";
    }

    private async Task SendApprovalEmailAsync(BeachTokenAdminDto token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token.CompetitorEmail) || string.IsNullOrWhiteSpace(token.TokenCode))
        {
            return;
        }

        var settingsJson = await adminSettingsRepository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken);
        var settings = AdminSettingsSerializer.DeserializeOrDefault(settingsJson);
        var textBody = BuildTokenEmailText(token, settings.Notifications.CompetitorTokenEmailTemplate);

        await emailSender.SendAsync(
            new EmailMessage(
                token.CompetitorEmail,
                $"Token de pago para {token.Event}",
                textBody,
                BuildTokenEmailHtml(token, textBody)),
            cancellationToken);
    }

    private static string BuildTokenEmailText(BeachTokenAdminDto token, string template)
    {
        var body = string.IsNullOrWhiteSpace(template)
            ? AdminSettingsDefaults.Create().Notifications.CompetitorTokenEmailTemplate
            : template;

        return body
            .Replace("[EVENTO]", token.Event, StringComparison.OrdinalIgnoreCase)
            .Replace("[TOKEN]", token.TokenCode ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("[CATEGORIA]", token.Category, StringComparison.OrdinalIgnoreCase)
            .Replace("[COMPETIDOR]", token.CompetitorName, StringComparison.OrdinalIgnoreCase)
            .Replace("[MONTO]", token.AmountUsd.ToString("0.##"), StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildTokenEmailHtml(BeachTokenAdminDto token, string textBody)
    {
        var expiration = token.ExpiracionAt?.ToString("dd/MM/yyyy HH:mm") ?? "24 horas desde la aprobacion";

        return TransactionalEmailTemplate.Render(
            "Pago en playa",
            "Tu token de pago fue aprobado",
            textBody,
            "Codigo token",
            token.TokenCode ?? string.Empty,
            [
                new EmailDetail("Evento", token.Event),
                new EmailDetail("Categoria", token.Category),
                new EmailDetail("Competidor", token.CompetitorName),
                new EmailDetail("Monto", $"USD {token.AmountUsd:0.##}"),
                new EmailDetail("Valido hasta", expiration)
            ],
            "Usa este codigo en la web, sección 'Mis Inscripciones' para completar tu inscripción con pago en efectivo. El token es personal y vence a las 24 horas.",
            "Este mensaje fue enviado automaticamente por ALAS Global Tour.");
    }
}
