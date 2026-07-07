using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;

namespace AlasApp.Application.Competitors.Queries.GetCompetitorNotifications;

public sealed class GetCompetitorNotificationsQueryHandler(ICompetitorRepository competitorRepository)
    : IRequestHandler<GetCompetitorNotificationsQuery, NotificationPreferencesDto>
{
    public async Task<NotificationPreferencesDto> Handle(GetCompetitorNotificationsQuery request, CancellationToken cancellationToken)
    {
        var competitor = await competitorRepository.GetByIdAsync(request.CompetitorId, cancellationToken)
            ?? throw new NotFoundException("Competidor no encontrado.");

        return new NotificationPreferencesDto(
            competitor.NotificationEmail,
            competitor.NotificationPush,
            competitor.NotificationResultados,
            competitor.NotificationInscripciones,
            true);
    }
}
