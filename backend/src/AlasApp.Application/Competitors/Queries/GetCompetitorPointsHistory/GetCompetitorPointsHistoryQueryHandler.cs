using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;

namespace AlasApp.Application.Competitors.Queries.GetCompetitorPointsHistory;

public sealed class GetCompetitorPointsHistoryQueryHandler(ICompetitorRepository competitorRepository)
    : IRequestHandler<GetCompetitorPointsHistoryQuery, CompetitorPointsHistoryDto>
{
    public async Task<CompetitorPointsHistoryDto> Handle(GetCompetitorPointsHistoryQuery request, CancellationToken cancellationToken)
    {
        _ = await competitorRepository.GetByIdAsync(request.CompetitorId, cancellationToken)
            ?? throw new NotFoundException("Competidor no encontrado.");

        return new CompetitorPointsHistoryDto(
            request.CompetitorId,
            request.Year ?? DateTime.UtcNow.Year,
            request.CategoryId ?? string.Empty,
            [],
            new CompetitorPointsHistoryStatsDto("-", 0, 0, 0, "-", "-"),
            "SurfScores");
    }
}
