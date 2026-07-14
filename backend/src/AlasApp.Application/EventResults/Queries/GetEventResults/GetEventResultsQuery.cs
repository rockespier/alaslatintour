using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.EventResults.Models;

namespace AlasApp.Application.EventResults.Queries.GetEventResults;

public sealed record GetEventResultsQuery(Guid EventId, Guid? CategoryId) : IRequest<IReadOnlyCollection<EventResultDto>>;
