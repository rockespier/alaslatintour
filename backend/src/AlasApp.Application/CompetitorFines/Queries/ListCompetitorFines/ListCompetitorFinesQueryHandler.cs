using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.CompetitorFines.Models;

namespace AlasApp.Application.CompetitorFines.Queries.ListCompetitorFines;

public sealed class ListCompetitorFinesQueryHandler(
    ICompetitorRepository competitorRepository,
    ICompetitorFineRepository competitorFineRepository)
    : IRequestHandler<ListCompetitorFinesQuery, IReadOnlyCollection<CompetitorFineDto>>
{
    public async Task<IReadOnlyCollection<CompetitorFineDto>> Handle(ListCompetitorFinesQuery request, CancellationToken cancellationToken)
    {
        _ = await competitorRepository.GetByIdAsync(request.CompetitorId, cancellationToken)
            ?? throw new NotFoundException("Competidor no encontrado.");

        return await competitorFineRepository.ListByCompetitorAsync(request.CompetitorId, cancellationToken);
    }
}
