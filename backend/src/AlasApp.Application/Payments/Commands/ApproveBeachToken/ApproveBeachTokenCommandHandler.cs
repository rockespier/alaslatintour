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
    IUserAccountRepository userAccountRepository,
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

            try
            {
                await SendApprovalEmailAsync(approvedToken, cancellationToken);
            }
            catch
            {
                // El token ya quedo aprobado; el correo es secundario y no debe revertir ni fallar la aprobacion.
            }

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
        var account = await userAccountRepository.GetByEmailAsync(token.CompetitorEmail, cancellationToken);
        var email = CompetitorEmails.BeachTokenApproved(EmailLanguage.From(account?.IdiomaPreferido), token, textBody);

        await emailSender.SendAsync(
            new EmailMessage(token.CompetitorEmail, email.Subject, email.Text, email.Html),
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
}
