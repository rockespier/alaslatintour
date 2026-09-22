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

        var rawEventRows = await dbContext.EventResults
            .AsNoTracking()
            .Where(x => x.Event != null && x.Event.CircuitId == circuitId)
            .Select(x => new
            {
                x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Nombre : string.Empty,
                BestResultsCount = x.Category != null ? x.Category.BestResultsCount : bestResultsCount,
                EventId = x.EventId,
                EventYear = x.Event != null ? x.Event.FechaInicio.Year : 0,
                x.CompetitorId,
                CompetitorName = x.Competitor != null ? x.Competitor.Nombre + " " + x.Competitor.Apellido : string.Empty,
                Country = x.Competitor != null ? x.Competitor.Pais : string.Empty,
                Points = x.Event != null ? x.Event.ApplyRankingBonus(x.LigaPoints) : x.LigaPoints
            })
            .ToListAsync(cancellationToken);

        // Membresia pagada: si el toggle esta activo, un competidor que no pago la membresia en su
        // inscripcion de un evento especifico recibe 0 puntos SOLO para ese evento (no se lo excluye
        // del ranking ni se afectan sus otros eventos con membresia si paga).
        var paidMembershipByEvent = settings.Ranking.ExcludeCompetitorsWithoutMembership
            ? (await dbContext.Inscriptions.AsNoTracking()
                .Where(x => x.Event != null && x.Event.CircuitId == circuitId
                    && x.EstadoAdmin == Domain.Enums.InscriptionStatusAdmin.Pagado
                    && x.MembershipPlan != null && x.MembershipFeeUsd > 0)
                .Select(x => new { x.CompetitorId, x.EventId })
                .ToListAsync(cancellationToken))
                .Select(x => (x.CompetitorId, x.EventId))
                .ToHashSet()
            : null;

        var eventRows = rawEventRows
            .Select(x => new
            {
                x.CategoryId,
                x.CategoryName,
                x.BestResultsCount,
                x.EventId,
                x.EventYear,
                x.CompetitorId,
                x.CompetitorName,
                x.Country,
                Points = paidMembershipByEvent is not null && !paidMembershipByEvent.Contains((x.CompetitorId, x.EventId))
                    ? 0
                    : x.Points
            })
            .ToList();

        var rankings = eventRows
            .Where(x => x.CategoryId != Guid.Empty && x.EventYear > 0)
            .GroupBy(x => new { x.CategoryId, x.CategoryName, x.EventYear })
            .Select(group =>
            {
                var categoryBestResultsCount = group.Max(x => x.BestResultsCount);
                var stageCount = group.Select(x => x.EventId).Distinct().Count();
                var resultsToCount = settings.Ranking.UseStageDropPercentageFormula
                    ? Math.Max(1, stageCount - (int)Math.Floor(stageCount * 0.30m + 0.5m))
                    : categoryBestResultsCount;

                List<RankingEntryDto> entries;
                if (settings.Ranking.UseStageDropPercentageFormula)
                {
                    // Desempate por recuento progresivo: si dos o mas compiten con los mismos puntos
                    // usando "resultsToCount" mejores resultados, se recalcula con una etapa menos, luego
                    // dos menos, etc. hasta 1; si persiste, se prueba hacia arriba hasta considerar todas
                    // las etapas. Nota: el fallback final al ranking de la temporada anterior descrito en
                    // el plan NO esta implementado todavia (requiere resolver el circuito correspondiente
                    // a year-1, fuera del alcance resuelto en esta sesion) — se documenta como pendiente.
                    var tieBreakKs = new List<int>();
                    for (var k = resultsToCount; k >= 1; k--) tieBreakKs.Add(k);
                    for (var k = resultsToCount + 1; k <= stageCount; k++) tieBreakKs.Add(k);

                    var perCompetitor = group
                        .GroupBy(x => new { x.CompetitorName, x.Country })
                        .Select(c => new
                        {
                            c.Key.CompetitorName,
                            c.Key.Country,
                            SortedPoints = c.Select(x => x.Points).OrderByDescending(p => p).ToList(),
                            Events = c.Select(x => x.EventId).Distinct().Count()
                        })
                        .Select(c => new
                        {
                            c.CompetitorName,
                            c.Country,
                            c.Events,
                            Points = c.SortedPoints.Take(resultsToCount).Sum(),
                            TieBreakSums = tieBreakKs.Select(k => c.SortedPoints.Take(k).Sum()).ToList()
                        })
                        .ToList();

                    perCompetitor.Sort((a, b) =>
                    {
                        for (var i = 0; i < tieBreakKs.Count; i++)
                        {
                            var cmp = b.TieBreakSums[i].CompareTo(a.TieBreakSums[i]);
                            if (cmp != 0) return cmp;
                        }

                        var eventsCmp = b.Events.CompareTo(a.Events);
                        return eventsCmp != 0 ? eventsCmp : string.Compare(a.CompetitorName, b.CompetitorName, StringComparison.Ordinal);
                    });

                    entries = perCompetitor
                        .Select((x, index) => new RankingEntryDto(
                            index + 1,
                            x.CompetitorName,
                            x.Country,
                            x.Points,
                            x.Events,
                            index == 0 ? 0 : -index))
                        .ToList();
                }
                else
                {
                    entries = group
                        .GroupBy(x => new { x.CompetitorName, x.Country })
                        .Select(competitor => new
                        {
                            competitor.Key.CompetitorName,
                            competitor.Key.Country,
                            Points = competitor
                                .OrderByDescending(x => x.Points)
                                .Take(resultsToCount)
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
                }

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
