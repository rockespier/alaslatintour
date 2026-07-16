using AlasApp.Application.Common;
using AlasApp.Application.Events.Models;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.Abstractions.Persistence;

public interface IEventRepository
{
    Task<PagedResult<EventDto>> ListAsync(EventListFilter filter, CancellationToken cancellationToken);

    Task<EventDto?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken);

    Task<Event?> GetEntityByIdAsync(Guid eventId, CancellationToken cancellationToken);

    Task<Event?> GetEntityBySurfScoresCodeAsync(string surfScoresCode, CancellationToken cancellationToken);

    Task AddAsync(Event @event, CancellationToken cancellationToken);

    void Remove(Event @event);
}
