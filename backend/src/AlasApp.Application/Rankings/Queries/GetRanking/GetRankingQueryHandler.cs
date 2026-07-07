using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;

namespace AlasApp.Application.Rankings.Queries.GetRanking;

public sealed class GetRankingQueryHandler(IRankingRepository rankingRepository, IClock clock)
    : IRequestHandler<GetRankingQuery, Models.RankingDto>
{
    public async Task<Models.RankingDto> Handle(GetRankingQuery request, CancellationToken cancellationToken)
    {
        var year = request.Year ?? clock.UtcNow.Year;
        var page = request.Page.GetValueOrDefault(1);
        var limit = request.Limit.GetValueOrDefault(20);

        var ranking = await rankingRepository.GetAsync(request.CategoryId, year, page, limit, cancellationToken)
            ?? throw new NotFoundException("No existe ranking cacheado para la categoria y temporada solicitadas.");

        return ranking;
    }
}
