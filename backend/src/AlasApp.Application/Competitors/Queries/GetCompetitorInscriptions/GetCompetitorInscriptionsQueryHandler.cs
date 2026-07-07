using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;

namespace AlasApp.Application.Competitors.Queries.GetCompetitorInscriptions;

public sealed class GetCompetitorInscriptionsQueryHandler(
    ICompetitorRepository competitorRepository,
    IInscriptionRepository inscriptionRepository)
    : IRequestHandler<GetCompetitorInscriptionsQuery, PagedResult<CompetitorInscriptionDto>>
{
    public async Task<PagedResult<CompetitorInscriptionDto>> Handle(GetCompetitorInscriptionsQuery request, CancellationToken cancellationToken)
    {
        _ = await competitorRepository.GetByIdAsync(request.CompetitorId, cancellationToken)
            ?? throw new NotFoundException("Competidor no encontrado.");

        return await inscriptionRepository.ListByCompetitorAsync(request.CompetitorId, request.Status, cancellationToken);
    }
}
