using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.AdminSettings;
using AlasApp.Application.Common;

namespace AlasApp.Application.Rankings.Queries.GetRanking;

public sealed class GetRankingQueryHandler(
    IRankingRepository rankingRepository,
    ICircuitRepository circuitRepository,
    IAdminSettingsRepository adminSettingsRepository)
    : IRequestHandler<GetRankingQuery, Models.RankingDto>
{
    public async Task<Models.RankingDto> Handle(GetRankingQuery request, CancellationToken cancellationToken)
    {
        var settingsJson = await adminSettingsRepository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken);
        var settings = AdminSettingsSerializer.DeserializeOrDefault(settingsJson);
        var seasonYear = settings.General.Season.CurrentYear;
        var year = request.Year ?? seasonYear;
        var page = request.Page.GetValueOrDefault(1);
        var limit = request.Limit.GetValueOrDefault(20);
        var currentCircuit = await circuitRepository.GetCurrentBySeasonAsync(seasonYear, cancellationToken)
            ?? throw new NotFoundException("No existe un circuito actual configurado para la temporada.");

        var ranking = await rankingRepository.GetAsync(currentCircuit.Id, request.CategoryId, year, page, limit, cancellationToken)
            ?? throw new NotFoundException("No existe ranking cacheado para la categoria y temporada solicitadas.");

        return ranking;
    }
}
