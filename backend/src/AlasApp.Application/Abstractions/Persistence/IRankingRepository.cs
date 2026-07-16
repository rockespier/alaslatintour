using AlasApp.Application.Rankings.Models;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.Abstractions.Persistence;

public interface IRankingRepository
{
    Task<RankingDto?> GetAsync(Guid circuitId, Guid categoryId, int year, int page, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RankingCategoryAvailabilityDto>> ListAvailableCategoriesAsync(Guid circuitId, CancellationToken cancellationToken);

    Task ReplaceCircuitSnapshotsAsync(
        Guid circuitId,
        IReadOnlyCollection<RankingSnapshot> snapshots,
        CancellationToken cancellationToken);
}
