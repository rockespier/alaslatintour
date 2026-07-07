using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Competitors.Models;

namespace AlasApp.Application.Competitors.Commands.UpdateCompetitorNotifications;

public sealed record UpdateCompetitorNotificationsCommand(
    Guid CompetitorId,
    bool Email,
    bool Push,
    bool Resultados,
    bool Inscripciones) : IRequest<NotificationPreferencesDto>;
