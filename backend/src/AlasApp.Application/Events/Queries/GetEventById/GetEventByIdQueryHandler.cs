using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Events.Models;

namespace AlasApp.Application.Events.Queries.GetEventById;

public sealed class GetEventByIdQueryHandler(IEventRepository eventRepository)
    : IRequestHandler<GetEventByIdQuery, EventDto>
{
    public async Task<EventDto> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);

        return @event ?? throw new NotFoundException("Evento no encontrado.");
    }
}
