using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Events.Models;

namespace AlasApp.Application.Events.Queries.ListEvents;

public sealed class ListEventsQueryHandler(IEventRepository eventRepository)
    : IRequestHandler<ListEventsQuery, PagedResult<EventDto>>
{
    public Task<PagedResult<EventDto>> Handle(ListEventsQuery request, CancellationToken cancellationToken)
    {
        return eventRepository.ListAsync(request.Filter, cancellationToken);
    }
}
