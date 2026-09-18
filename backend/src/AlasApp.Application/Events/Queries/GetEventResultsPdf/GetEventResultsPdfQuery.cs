using AlasApp.Application.Abstractions.Messaging;

namespace AlasApp.Application.Events.Queries.GetEventResultsPdf;

public sealed record GetEventResultsPdfQuery(Guid EventId) : IRequest<string?>;
