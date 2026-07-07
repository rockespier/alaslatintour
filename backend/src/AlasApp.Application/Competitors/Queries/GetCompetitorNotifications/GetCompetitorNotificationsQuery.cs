using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Competitors.Models;

namespace AlasApp.Application.Competitors.Queries.GetCompetitorNotifications;

public sealed record GetCompetitorNotificationsQuery(Guid CompetitorId) : IRequest<NotificationPreferencesDto>;
