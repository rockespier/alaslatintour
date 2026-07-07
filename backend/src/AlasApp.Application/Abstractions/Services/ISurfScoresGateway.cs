using AlasApp.Application.Rankings.Models;

namespace AlasApp.Application.Abstractions.Services;

public interface ISurfScoresGateway
{
    Task<IReadOnlyCollection<SurfScoresRankingSnapshotDto>> BuildCircuitRankingCacheAsync(
        Guid circuitId,
        CancellationToken cancellationToken);
}
