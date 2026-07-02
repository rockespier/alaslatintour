using AlasApp.Application.Abstractions.Messaging;

namespace AlasApp.Application.Events.Commands.DeleteEvent;

public sealed record DeleteEventCommand(Guid EventId) : IRequest<bool>;
