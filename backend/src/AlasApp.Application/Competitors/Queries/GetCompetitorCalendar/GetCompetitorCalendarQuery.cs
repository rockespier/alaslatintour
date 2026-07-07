using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Competitors.Models;

namespace AlasApp.Application.Competitors.Queries.GetCompetitorCalendar;

public sealed record GetCompetitorCalendarQuery(Guid CompetitorId) : IRequest<IReadOnlyCollection<CompetitorCalendarEventDto>>;
