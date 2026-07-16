using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminSettings;
using AlasApp.Application.Rankings.Models;
using AlasApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.SurfScores;

public sealed class SurfScoresGateway(
    AlasAppDbContext dbContext,
    IAdminSettingsRepository settingsRepository) : ISurfScoresGateway
{
    public async Task<IReadOnlyCollection<SurfScoresRankingSnapshotDto>> BuildCircuitRankingCacheAsync(
        Guid circuitId,
        CancellationToken cancellationToken)
    {
        var settingsJson = await settingsRepository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken);
        var settings = AdminSettingsSerializer.DeserializeOrDefault(settingsJson);
        var bestResultsCount = settings.Ranking.BestResultsCount;

        var eventRows = await dbContext.EventResults
            .AsNoTracking()
            .Where(x => x.Event != null && x.Event.CircuitId == circuitId)
            .Select(x => new
            {
                x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Nombre : string.Empty,
                BestResultsCount = x.Category != null ? x.Category.BestResultsCount : bestResultsCount,
                EventId = x.EventId,
                EventYear = x.Event != null ? x.Event.FechaInicio.Year : 0,
                CompetitorName = x.Competitor != null ? x.Competitor.Nombre + " " + x.Competitor.Apellido : string.Empty,
                Country = x.Competitor != null ? x.Competitor.Pais : string.Empty,
                Points = x.Event != null ? x.Event.ApplyRankingBonus(x.LigaPoints) : x.LigaPoints
            })
            .ToListAsync(cancellationToken);

        var rankings = eventRows
            .Where(x => x.CategoryId != Guid.Empty && x.EventYear > 0)
            .GroupBy(x => new { x.CategoryId, x.CategoryName, x.EventYear })
            .Select(group =>
            {
                var categoryBestResultsCount = group.Max(x => x.BestResultsCount);
                var entries = group
                    .GroupBy(x => new { x.CompetitorName, x.Country })
                    .Select(competitor => new
                    {
                        competitor.Key.CompetitorName,
                        competitor.Key.Country,
                        Points = competitor
                            .OrderByDescending(x => x.Points)
                            .Take(categoryBestResultsCount)
                            .Sum(x => x.Points),
                        Events = competitor.Select(x => x.EventId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.Points)
                    .ThenByDescending(x => x.Events)
                    .ThenBy(x => x.CompetitorName)
                    .Select((x, index) => new RankingEntryDto(
                        index + 1,
                        x.CompetitorName,
                        x.Country,
                        x.Points,
                        x.Events,
                        index == 0 ? 0 : -index))
                    .ToList();

                return new SurfScoresRankingSnapshotDto(
                    group.Key.CategoryId,
                    string.IsNullOrWhiteSpace(group.Key.CategoryName) ? "Categoria" : group.Key.CategoryName,
                    group.Key.EventYear,
                    entries);
            })
            .Where(x => x.Entries.Count > 0)
            .ToList();

        return rankings;
    }
}
