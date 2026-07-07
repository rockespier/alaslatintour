using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Rankings.Models;

namespace AlasApp.Application.Rankings.Queries.ListRankingCategories;

public sealed class ListRankingCategoriesQueryHandler(IRankingRepository rankingRepository)
    : IRequestHandler<ListRankingCategoriesQuery, IReadOnlyCollection<RankingCategoryAvailabilityDto>>
{
    public Task<IReadOnlyCollection<RankingCategoryAvailabilityDto>> Handle(
        ListRankingCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        return rankingRepository.ListAvailableCategoriesAsync(cancellationToken);
    }
}
