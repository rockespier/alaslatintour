using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Rankings.Models;
using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class RankingRepository(AlasAppDbContext dbContext) : IRankingRepository
{
    public async Task<RankingDto?> GetAsync(Guid categoryId, int year, int page, int limit, CancellationToken cancellationToken)
    {
        page = page <= 0 ? 1 : page;
        limit = limit <= 0 ? 20 : limit;

        var snapshot = await dbContext.RankingSnapshots
            .AsNoTracking()
            .Include(x => x.Entries.OrderBy(e => e.Position))
            .Where(x => x.CategoryId == categoryId && x.Year == year)
            .OrderByDescending(x => x.CachedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        var totalItems = snapshot.Entries.Count;
        var entries = snapshot.Entries
            .OrderBy(x => x.Position)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new RankingEntryDto(x.Position, x.CompetitorName, x.Country, x.Points, x.Events, x.Variation))
            .ToList();

        return new RankingDto(
            snapshot.CategoryId,
            snapshot.CategoryName,
            snapshot.Year,
            snapshot.CachedAtUtc,
            entries,
            new RankingPaginationDto(
                page,
                limit,
                totalItems,
                limit == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)limit)));
    }

    public async Task<IReadOnlyCollection<RankingCategoryAvailabilityDto>> ListAvailableCategoriesAsync(CancellationToken cancellationToken)
    {
        var snapshots = await dbContext.RankingSnapshots
            .AsNoTracking()
            .GroupBy(x => new { x.CategoryId, x.CategoryName })
            .Select(group => new RankingCategoryAvailabilityDto(
                group.Key.CategoryId,
                group.Key.CategoryName,
                group.Select(x => x.Year).Distinct().OrderByDescending(x => x).ToList()))
            .OrderBy(x => x.CategoryName)
            .ToListAsync(cancellationToken);

        return snapshots;
    }

    public async Task ReplaceCircuitSnapshotsAsync(
        Guid circuitId,
        IReadOnlyCollection<RankingSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.RankingSnapshots
            .Include(x => x.Entries)
            .Where(x => x.CircuitId == circuitId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            dbContext.RankingSnapshots.RemoveRange(existing);
        }

        if (snapshots.Count > 0)
        {
            await dbContext.RankingSnapshots.AddRangeAsync(snapshots, cancellationToken);
        }
    }
}
