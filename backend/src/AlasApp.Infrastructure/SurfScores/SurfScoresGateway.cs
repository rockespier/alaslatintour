using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Rankings.Models;
using AlasApp.Domain.Enums;
using AlasApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.SurfScores;

public sealed class SurfScoresGateway(AlasAppDbContext dbContext) : ISurfScoresGateway
{
    public async Task<IReadOnlyCollection<SurfScoresRankingSnapshotDto>> BuildCircuitRankingCacheAsync(
        Guid circuitId,
        CancellationToken cancellationToken)
    {
        var eventRows = await dbContext.Inscriptions
            .AsNoTracking()
            .Where(x => x.EventId != Guid.Empty && x.Event != null && x.Event.CircuitId == circuitId)
            .Where(x => x.EstadoAdmin == InscriptionStatusAdmin.Pagado)
            .Select(x => new
            {
                x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Nombre : string.Empty,
                EventId = x.EventId,
                EventYear = x.Event != null ? x.Event.FechaInicio.Year : 0,
                CompetitorName = x.Competitor != null ? x.Competitor.Nombre + " " + x.Competitor.Apellido : string.Empty,
                Country = x.Competitor != null ? x.Competitor.Pais : string.Empty,
                Points = (x.Event != null ? x.Event.Stars : 0) * 100
            })
            .ToListAsync(cancellationToken);

        var rankings = eventRows
            .Where(x => x.CategoryId != Guid.Empty && x.EventYear > 0)
            .GroupBy(x => new { x.CategoryId, x.CategoryName, x.EventYear })
            .Select(group =>
            {
                var entries = group
                    .GroupBy(x => new { x.CompetitorName, x.Country })
                    .Select(competitor => new
                    {
                        competitor.Key.CompetitorName,
                        competitor.Key.Country,
                        Points = competitor.Sum(x => x.Points),
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
