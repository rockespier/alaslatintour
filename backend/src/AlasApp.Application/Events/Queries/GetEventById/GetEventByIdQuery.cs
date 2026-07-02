using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Events.Models;

namespace AlasApp.Application.Events.Queries.GetEventById;

public sealed record GetEventByIdQuery(Guid EventId) : IRequest<EventDto>;
