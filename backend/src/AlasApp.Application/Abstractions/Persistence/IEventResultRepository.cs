using AlasApp.Application.EventResults.Models;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.Abstractions.Persistence;

public interface IEventResultRepository
{
    Task<IReadOnlyCollection<EventResultDto>> ListAsync(Guid eventId, Guid? categoryId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Guid>> ListRegisteredCompetitorIdsAsync(Guid eventId, Guid categoryId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<EventResult>> ListEntitiesAsync(Guid eventId, Guid categoryId, CancellationToken cancellationToken);

    Task AddAsync(EventResult result, CancellationToken cancellationToken);

    void Remove(EventResult result);
}
