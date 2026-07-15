using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminSettings;
using AlasApp.Application.AdminSettings.Models;
using AlasApp.Application.Common;
using AlasApp.Application.EventResults.Models;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.EventResults.Commands.UpsertEventResults;

public sealed class UpsertEventResultsCommandHandler(
    IEventRepository eventRepository,
    IEventResultRepository eventResultRepository,
    IInscriptionRepository inscriptionRepository,
    IAdminSettingsRepository settingsRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<UpsertEventResultsCommand, IReadOnlyCollection<EventResultDto>>
{
    public async Task<IReadOnlyCollection<EventResultDto>> Handle(UpsertEventResultsCommand request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetEntityByIdAsync(request.EventId, cancellationToken);
        if (@event is null)
        {
            throw new NotFoundException("Evento no encontrado.");
        }

        if (@event.Categories.All(x => x.CategoryId != request.CategoryId))
        {
            throw new ValidationException(
                "La categoria no esta habilitada para el evento.",
                [new ValidationError("categoryId", "La categoria no esta habilitada para el evento.")]);
        }

        ValidateRequest(request);

        var registeredCompetitors = await eventResultRepository.ListRegisteredCompetitorIdsAsync(request.EventId, request.CategoryId, cancellationToken);
        var registeredSet = registeredCompetitors.ToHashSet();
        var notRegistered = request.Results
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

        var existing = await eventResultRepository.ListEntitiesAsync(request.EventId, request.CategoryId, cancellationToken);
        var existingByCompetitor = existing.ToDictionary(x => x.CompetitorId);
        var requestedCompetitors = request.Results.Select(x => x.CompetitorId).ToHashSet();
        var timestamp = clock.UtcNow;

        foreach (var staleResult in existing.Where(x => !requestedCompetitors.Contains(x.CompetitorId)))
        {
            eventResultRepository.Remove(staleResult);
        }

        foreach (var item in request.Results)
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
            }
            else
            {
                await eventResultRepository.AddAsync(
                    EventResult.Create(
                        request.EventId,
                        request.CategoryId,
                        normalized.CompetitorId,
                        normalized.Place,
                        normalized.LigaPoints,
                        normalized.PrizeUsd,
                        normalized.HeatOla1,
                        normalized.HeatOla2,
                        timestamp),
                    cancellationToken);
            }
        }

        var inscriptions = await inscriptionRepository.ListEntitiesByEventCategoryAsync(request.EventId, request.CategoryId, cancellationToken);
        foreach (var inscription in inscriptions)
        {
            var result = request.Results.FirstOrDefault(x => x.CompetitorId == inscription.CompetitorId);
            if (result is not null)
            {
                inscription.ApplyResult(result.Place, timestamp);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await eventResultRepository.ListAsync(request.EventId, request.CategoryId, cancellationToken);
    }

    private static void ValidateRequest(UpsertEventResultsCommand request)
    {
        if (request.Results.Count == 0)
        {
            throw new ValidationException(
                "Debe enviar al menos un resultado.",
                [new ValidationError("results", "Debe enviar al menos un resultado.")]);
        }

        var duplicateCompetitors = request.Results
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

        var duplicatePlaces = request.Results
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
        var prizeUsd = item.PrizeUsd ?? (prizeDistribution.TryGetValue(place, out var configuredPrize) ? configuredPrize : null);

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
            row => row.PlaceLabel,
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
