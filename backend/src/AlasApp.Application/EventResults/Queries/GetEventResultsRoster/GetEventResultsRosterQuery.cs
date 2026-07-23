using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.EventResults.Models;

namespace AlasApp.Application.EventResults.Queries.GetEventResultsRoster;

public sealed record GetEventResultsRosterQuery(Guid EventId, Guid CategoryId) : IRequest<IReadOnlyCollection<EventResultRosterRowDto>>;
