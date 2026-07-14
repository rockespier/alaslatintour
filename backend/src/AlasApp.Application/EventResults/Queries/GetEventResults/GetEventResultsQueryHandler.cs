using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.EventResults.Models;

namespace AlasApp.Application.EventResults.Queries.GetEventResults;

public sealed class GetEventResultsQueryHandler(
    IEventRepository eventRepository,
    IEventResultRepository eventResultRepository)
    : IRequestHandler<GetEventResultsQuery, IReadOnlyCollection<EventResultDto>>
{
    public async Task<IReadOnlyCollection<EventResultDto>> Handle(GetEventResultsQuery request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null)
        {
            throw new NotFoundException("Evento no encontrado.");
        }

        return await eventResultRepository.ListAsync(request.EventId, request.CategoryId, cancellationToken);
    }
}
