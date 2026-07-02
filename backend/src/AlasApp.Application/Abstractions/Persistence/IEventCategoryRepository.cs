using AlasApp.Application.EventCategories.Models;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.Abstractions.Persistence;

public interface IEventCategoryRepository
{
    Task<EventCategoryListDto?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<EventCategory>> BuildAssignmentsAsync(
        Guid eventId,
        IReadOnlyCollection<EventCategoryUpsertItem> items,
        CancellationToken cancellationToken);
}
