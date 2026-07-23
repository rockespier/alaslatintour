using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminSettings;
using AlasApp.Application.AdminSettings.Models;
using AlasApp.Application.Common;
using AlasApp.Application.EventResults.Models;
using AlasApp.Application.Rankings;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.EventResults;

public sealed record EventResultsWriteOutcome(
    IReadOnlyCollection<EventResultDto> Results,
    int CreatedCount,
    int UpdatedCount);

/// <summary>
/// Aplica un conjunto de resultados de evento (guardado manual o importacion masiva desde Excel),
/// manteniendo consistente el calculo de puntos/premios, el estado de las inscripciones y el
/// refresco del snapshot de ranking entre ambos flujos.
/// </summary>
public sealed class EventResultsWriter(
    IEventRepository eventRepository,
    IEventResultRepository eventResultRepository,
    IInscriptionRepository inscriptionRepository,
    IAdminSettingsRepository settingsRepository,
    IRankingRepository rankingRepository,
    ISurfScoresGateway surfScoresGateway,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<EventResultsWriteOutcome> UpsertAsync(
        Guid eventId,
        Guid categoryId,
        IReadOnlyCollection<EventResultUpsertItem> results,
        CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetEntityByIdAsync(eventId, cancellationToken);
        if (@event is null)
        {
            throw new NotFoundException("Evento no encontrado.");
        }

        if (@event.Categories.All(x => x.CategoryId != categoryId))
        {
            throw new ValidationException(
                "La categoria no esta habilitada para el evento.",
                [new ValidationError("categoryId", "La categoria no esta habilitada para el evento.")]);
        }

        ValidateRequest(results);

        var registeredCompetitors = await eventResultRepository.ListRegisteredCompetitorIdsAsync(eventId, categoryId, cancellationToken);
        var registeredSet = registeredCompetitors.ToHashSet();
        var notRegistered = results
            .Where(x => !registeredSet.Contains(x.CompetitorId))
            .Select(x => x.CompetitorId)
            .ToList();

        if (notRegistered.Count > 0)
        {
            throw new ValidationException(
                "Todos los competidores con resultado deben estar inscritos en el evento y categoria.",
                notRegistered
                    .Select(id => new ValidationError("results.competitorId", $"Competidor no inscrito: {id}"))
                    .ToList());
        }

        var json = await settingsRepository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken);
        var settings = AdminSettingsSerializer.DeserializeOrDefault(json);
        var prizeDistribution = BuildPrizeDistribution(@event.PrizeAmountUsd, @event.Stars, settings);

        var existing = await eventResultRepository.ListEntitiesAsync(eventId, categoryId, cancellationToken);
        var existingByCompetitor = existing.ToDictionary(x => x.CompetitorId);
        var requestedCompetitors = results.Select(x => x.CompetitorId).ToHashSet();
        var timestamp = clock.UtcNow;

        foreach (var staleResult in existing.Where(x => !requestedCompetitors.Contains(x.CompetitorId)))
        {
            eventResultRepository.Remove(staleResult);
        }

        var created = 0;
        var updated = 0;

        foreach (var item in results)
        {
            var normalized = Normalize(item, @event.Stars, settings, prizeDistribution);

            if (existingByCompetitor.TryGetValue(normalized.CompetitorId, out var existingResult))
            {
                existingResult.Update(
                    normalized.Place,
                    normalized.LigaPoints,
                    normalized.PrizeUsd,
                    normalized.HeatOla1,
                    normalized.HeatOla2,
                    timestamp);
                updated++;
            }
            else
            {
                await eventResultRepository.AddAsync(
                    EventResult.Create(
                        eventId,
                        categoryId,
                        normalized.CompetitorId,
                        normalized.Place,
                        normalized.LigaPoints,
                        normalized.PrizeUsd,
                        normalized.HeatOla1,
                        normalized.HeatOla2,
                        timestamp),
                    cancellationToken);
                created++;
            }
        }

        var inscriptions = await inscriptionRepository.ListEntitiesByEventCategoryAsync(eventId, categoryId, cancellationToken);
        foreach (var inscription in inscriptions)
        {
            var result = results.FirstOrDefault(x => x.CompetitorId == inscription.CompetitorId);
            if (result is not null)
            {
                inscription.ApplyResult(result.Place, timestamp);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var externalSnapshots = await surfScoresGateway.BuildCircuitRankingCacheAsync(@event.CircuitId, cancellationToken);
        var snapshots = RankingSnapshotFactory.Build(@event.CircuitId, externalSnapshots, timestamp);
        await rankingRepository.ReplaceCircuitSnapshotsAsync(@event.CircuitId, snapshots, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedResults = await eventResultRepository.ListAsync(eventId, categoryId, cancellationToken);
        return new EventResultsWriteOutcome(updatedResults, created, updated);
    }

    private static void ValidateRequest(IReadOnlyCollection<EventResultUpsertItem> results)
    {
        if (results.Count == 0)
        {
            throw new ValidationException(
                "Debe enviar al menos un resultado.",
                [new ValidationError("results", "Debe enviar al menos un resultado.")]);
        }

        var duplicateCompetitors = results
            .GroupBy(x => x.CompetitorId)
            .Where(x => x.Key == Guid.Empty || x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicateCompetitors.Count > 0)
        {
            throw new ValidationException(
                "No se permiten competidores duplicados en resultados.",
                duplicateCompetitors
                    .Select(id => new ValidationError("results.competitorId", $"Competidor duplicado o invalido: {id}"))
                    .ToList());
        }

        var duplicatePlaces = results
            .GroupBy(x => x.Place.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(x => string.IsNullOrWhiteSpace(x.Key) || x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicatePlaces.Count > 0)
        {
            throw new ValidationException(
                "No se permiten puestos duplicados.",
                duplicatePlaces
                    .Select(place => new ValidationError("results.place", $"Puesto duplicado o invalido: {place}"))
                    .ToList());
        }
    }

    private static EventResultUpsertItem Normalize(
        EventResultUpsertItem item,
        int stars,
        AdminSettingsDto settings,
        IReadOnlyDictionary<string, decimal> prizeDistribution)
    {
        var place = item.Place.Trim();
        var ligaPoints = item.LigaPoints > 0
            ? item.LigaPoints
            : ResolvePoints(place, stars, settings);
        var prizeUsd = item.PrizeUsd ?? (prizeDistribution.TryGetValue(NormalizePlace(place), out var configuredPrize) ? configuredPrize : null);

        return item with
        {
            Place = place,
            LigaPoints = ligaPoints,
            PrizeUsd = prizeUsd
        };
    }

    private static int ResolvePoints(string place, int stars, AdminSettingsDto settings)
    {
        var normalizedPlace = NormalizePlace(place);
        var row = settings.Ranking.PointsMatrix
            .FirstOrDefault(x => NormalizePlace(x.Position) == normalizedPlace);

        if (row is null)
        {
            return 0;
        }

        return stars switch
        {
            1 => row.Star1,
            2 => row.Star2,
            3 => row.Star3,
            4 => row.Star4,
            5 => row.Star5,
            6 => row.Star6,
            7 => row.Star7,
            _ => 0
        };
    }

    private static Dictionary<string, decimal> BuildPrizeDistribution(decimal totalPrizeUsd, int stars, AdminSettingsDto settings)
    {
        return settings.Ranking.PrizeDistribution.ToDictionary(
            row => NormalizePlace(row.PlaceLabel),
            row => Math.Round(totalPrizeUsd * GetPrizePercent(row, stars) / 100m, 2),
            StringComparer.OrdinalIgnoreCase);
    }

    private static decimal GetPrizePercent(PrizeDistributionSettingsDto row, int stars)
    {
        return stars switch
        {
            1 => row.Star1Percent,
            2 => row.Star2Percent,
            3 => row.Star3Percent,
            4 => row.Star4Percent,
            5 => row.Star5Percent,
            6 => row.Star6Percent,
            7 => row.Star7Percent,
            _ => 0
        };
    }

    private static string NormalizePlace(string place)
    {
        return place.Trim().Replace("°", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }
}
