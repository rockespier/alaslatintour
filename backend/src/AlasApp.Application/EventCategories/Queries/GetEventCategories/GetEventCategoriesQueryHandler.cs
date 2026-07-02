using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.EventCategories.Models;

namespace AlasApp.Application.EventCategories.Queries.GetEventCategories;

public sealed class GetEventCategoriesQueryHandler(IEventCategoryRepository eventCategoryRepository)
    : IRequestHandler<GetEventCategoriesQuery, EventCategoryListDto>
{
    public async Task<EventCategoryListDto> Handle(GetEventCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await eventCategoryRepository.GetByEventIdAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException("Evento no encontrado.");
    }
}
