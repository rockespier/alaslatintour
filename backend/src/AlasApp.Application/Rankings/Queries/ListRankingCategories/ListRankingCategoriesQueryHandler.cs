using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.AdminSettings;
using AlasApp.Application.Common;
using AlasApp.Application.Rankings.Models;

namespace AlasApp.Application.Rankings.Queries.ListRankingCategories;

public sealed class ListRankingCategoriesQueryHandler(
    IRankingRepository rankingRepository,
    ICircuitRepository circuitRepository,
    IAdminSettingsRepository adminSettingsRepository)
    : IRequestHandler<ListRankingCategoriesQuery, IReadOnlyCollection<RankingCategoryAvailabilityDto>>
{
    public async Task<IReadOnlyCollection<RankingCategoryAvailabilityDto>> Handle(
        ListRankingCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var settingsJson = await adminSettingsRepository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken);
        var settings = AdminSettingsSerializer.DeserializeOrDefault(settingsJson);
        var currentCircuit = await circuitRepository.GetCurrentBySeasonAsync(settings.General.Season.CurrentYear, cancellationToken)
            ?? throw new NotFoundException("No existe un circuito actual configurado para la temporada.");

        return await rankingRepository.ListAvailableCategoriesAsync(currentCircuit.Id, cancellationToken);
    }
}
