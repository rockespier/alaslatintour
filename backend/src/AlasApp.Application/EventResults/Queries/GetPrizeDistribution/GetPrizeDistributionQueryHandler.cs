using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.AdminSettings;
using AlasApp.Application.AdminSettings.Models;
using AlasApp.Application.Common;
using AlasApp.Application.EventResults.Models;

namespace AlasApp.Application.EventResults.Queries.GetPrizeDistribution;

public sealed class GetPrizeDistributionQueryHandler(
    IEventRepository eventRepository,
    IAdminSettingsRepository settingsRepository)
    : IRequestHandler<GetPrizeDistributionQuery, PrizeDistributionDto>
{
    public async Task<PrizeDistributionDto> Handle(GetPrizeDistributionQuery request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null)
        {
            throw new NotFoundException("Evento no encontrado.");
        }

        var json = await settingsRepository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken);
        var settings = AdminSettingsSerializer.DeserializeOrDefault(json);

        var rows = settings.Ranking.PrizeDistribution
            .Select(row => new PrizeDistributionRowDto(
                row.PlaceLabel,
                Math.Round(@event.PrizeAmountUsd * GetPercent(row, @event.Stars) / 100m, 2)))
            .Where(row => row.PrizeUsd > 0)
            .ToList();

        return new PrizeDistributionDto(@event.Stars, rows);
    }

    private static decimal GetPercent(PrizeDistributionSettingsDto row, int stars)
    {
        return stars switch
        {
            1 => row.Star1Percent,
            2 => row.Star2Percent,
            3 => row.Star3Percent,
            4 => row.Star4Percent,
            5 => row.Star5Percent,
            _ => 0
        };
    }
}
