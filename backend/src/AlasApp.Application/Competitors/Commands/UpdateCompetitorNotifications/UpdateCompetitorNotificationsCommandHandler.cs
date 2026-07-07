using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;

namespace AlasApp.Application.Competitors.Commands.UpdateCompetitorNotifications;

public sealed class UpdateCompetitorNotificationsCommandHandler(
    ICompetitorRepository competitorRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<UpdateCompetitorNotificationsCommand, NotificationPreferencesDto>
{
    public async Task<NotificationPreferencesDto> Handle(UpdateCompetitorNotificationsCommand request, CancellationToken cancellationToken)
    {
        var competitor = await competitorRepository.GetEntityByIdAsync(request.CompetitorId, cancellationToken)
            ?? throw new NotFoundException("Competidor no encontrado.");

        competitor.UpdateNotificationPreferences(request.Email, request.Push, request.Resultados, request.Inscripciones);
        competitor.SetUpdated(clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new NotificationPreferencesDto(
            competitor.NotificationEmail,
            competitor.NotificationPush,
            competitor.NotificationResultados,
            competitor.NotificationInscripciones,
            true);
    }
}
