using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;

namespace AlasApp.Application.Competitors.Queries.ListCompetitors;

public sealed class ListCompetitorsQueryHandler(ICompetitorRepository competitorRepository)
    : IRequestHandler<ListCompetitorsQuery, PagedResult<CompetitorDto>>
{
    public Task<PagedResult<CompetitorDto>> Handle(ListCompetitorsQuery request, CancellationToken cancellationToken)
    {
        return competitorRepository.ListAsync(request.Filter, cancellationToken);
    }
}
