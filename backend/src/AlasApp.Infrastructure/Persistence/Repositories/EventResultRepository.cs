using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.EventResults.Models;
using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class EventResultRepository(AlasAppDbContext dbContext) : IEventResultRepository
{
    public async Task<IReadOnlyCollection<EventResultDto>> ListAsync(Guid eventId, Guid? categoryId, CancellationToken cancellationToken)
    {
        var query = dbContext.EventResults
            .AsNoTracking()
            .Include(x => x.Competitor)
            .Where(x => x.EventId == eventId);

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        var results = await query
            .OrderBy(x => x.CategoryId)
            .ThenByDescending(x => x.LigaPoints)
            .ThenBy(x => x.Place)
            .ToListAsync(cancellationToken);

        return results.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyCollection<Guid>> ListRegisteredCompetitorIdsAsync(Guid eventId, Guid categoryId, CancellationToken cancellationToken)
    {
        return await dbContext.Inscriptions
            .AsNoTracking()
            .Where(x => x.EventId == eventId && x.CategoryId == categoryId)
            .Select(x => x.CompetitorId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<EventResult>> ListEntitiesAsync(Guid eventId, Guid categoryId, CancellationToken cancellationToken)
    {
        return await dbContext.EventResults
            .Where(x => x.EventId == eventId && x.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(EventResult result, CancellationToken cancellationToken)
    {
        return dbContext.EventResults.AddAsync(result, cancellationToken).AsTask();
    }

    public void Remove(EventResult result)
    {
        dbContext.EventResults.Remove(result);
    }

    private static EventResultDto MapToDto(EventResult result)
    {
        var competitorName = result.Competitor is null
            ? string.Empty
            : $"{result.Competitor.Nombre} {result.Competitor.Apellido}";

        return new EventResultDto(
            result.Id,
            result.EventId,
            result.CategoryId,
            result.CompetitorId,
            competitorName,
            result.Competitor?.Pais ?? string.Empty,
            result.Place,
            result.LigaPoints,
            result.PrizeUsd,
            result.HeatOla1,
            result.HeatOla2,
            result.HeatScoreTotal);
    }
}
