using AlasApp.Application.Rankings.Models;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.Rankings;

public static class RankingSnapshotFactory
{
    public static List<RankingSnapshot> Build(
        Guid circuitId,
        IReadOnlyCollection<SurfScoresRankingSnapshotDto> externalSnapshots,
        DateTimeOffset syncedAt)
    {
        return externalSnapshots
            .Select(dto => ToDomainSnapshot(circuitId, dto, syncedAt))
            .ToList();
    }

    private static RankingSnapshot ToDomainSnapshot(Guid circuitId, SurfScoresRankingSnapshotDto dto, DateTimeOffset syncedAt)
    {
        var snapshot = RankingSnapshot.Create(circuitId, dto.CategoryId, dto.CategoryName, dto.Year, syncedAt);
        snapshot.SetCreated(syncedAt);

        foreach (var entry in dto.Entries)
        {
            snapshot.AddEntry(entry.Name, entry.Country, entry.Pos, entry.Points, entry.Events, entry.Variation);
        }

        return snapshot;
    }
}
