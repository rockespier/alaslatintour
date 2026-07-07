using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;

namespace AlasApp.Application.Competitors.Queries.GetCompetitorById;

public sealed class GetCompetitorByIdQueryHandler(ICompetitorRepository competitorRepository)
    : IRequestHandler<GetCompetitorByIdQuery, CompetitorDto>
{
    public async Task<CompetitorDto> Handle(GetCompetitorByIdQuery request, CancellationToken cancellationToken)
    {
        return await competitorRepository.GetByIdAsync(request.CompetitorId, cancellationToken)
            ?? throw new NotFoundException("Competidor no encontrado.");
    }
}
