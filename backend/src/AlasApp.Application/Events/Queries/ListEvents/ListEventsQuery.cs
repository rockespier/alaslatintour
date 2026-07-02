using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Common;
using AlasApp.Application.Events.Models;

namespace AlasApp.Application.Events.Queries.ListEvents;

public sealed record ListEventsQuery(EventListFilter Filter) : IRequest<PagedResult<EventDto>>;
