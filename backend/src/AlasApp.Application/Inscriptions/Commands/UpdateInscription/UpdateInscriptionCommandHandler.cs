using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminSettings;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;
using AlasApp.Application.Emails;
using AlasApp.Application.Inscriptions.Models;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace AlasApp.Application.Inscriptions.Commands.UpdateInscription;

public sealed class UpdateInscriptionCommandHandler(
    IInscriptionRepository inscriptionRepository,
    ICompetitorRepository competitorRepository,
    IUserAccountRepository userAccountRepository,
    IAdminSettingsRepository adminSettingsRepository,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<UpdateInscriptionCommandHandler> logger)
    : IRequestHandler<UpdateInscriptionCommand, InscriptionDto>
{
    public async Task<InscriptionDto> Handle(UpdateInscriptionCommand request, CancellationToken cancellationToken)
    {
        var inscription = await inscriptionRepository.GetEntityByIdAsync(request.InscriptionId, cancellationToken)
            ?? throw new NotFoundException("Inscripcion no encontrada.");

        var wasAlreadyPaid = inscription.EstadoAdmin == InscriptionStatusAdmin.Pagado;

        try
        {
            inscription.Update(request.ShirtNumber, request.EstadoAdmin, request.Notes);
            inscription.SetUpdated(clock.UtcNow);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = await inscriptionRepository.GetByIdAsync(inscription.Id, cancellationToken)
                ?? throw new NotFoundException("Inscripcion no encontrada despues de actualizarla.");

            var nowPaid = inscription.EstadoAdmin == InscriptionStatusAdmin.Pagado;
            if (nowPaid && !wasAlreadyPaid)
            {
                await NotifyPaymentConfirmedAsync(inscription.CompetitorId, dto, cancellationToken);
            }

            return dto;
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private async Task NotifyPaymentConfirmedAsync(Guid competitorId, InscriptionDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var settingsJson = await adminSettingsRepository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken);
            var settings = AdminSettingsSerializer.DeserializeOrDefault(settingsJson);

            if (!settings.Notifications.NotifyConfirmedPayments)
            {
                return;
            }

            CompetitorDto? competitor = await competitorRepository.GetByIdAsync(competitorId, cancellationToken);
            if (competitor is null || string.IsNullOrWhiteSpace(competitor.Email))
            {
                return;
            }

            var account = await userAccountRepository.GetByCompetitorIdAsync(competitorId, cancellationToken);
            var email = CompetitorEmails.PaymentConfirmed(
                EmailLanguage.From(account?.IdiomaPreferido),
                competitor.Nombre,
                dto.Event.Nombre,
                dto.Category.Nombre,
                dto.MontoUsd);

            await emailSender.SendAsync(
                new EmailMessage(competitor.Email, email.Subject, email.Text, email.Html),
                cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "No se pudo enviar la notificación de pago confirmado para la inscripción {Id}.", dto.Id);
        }
    }
}
