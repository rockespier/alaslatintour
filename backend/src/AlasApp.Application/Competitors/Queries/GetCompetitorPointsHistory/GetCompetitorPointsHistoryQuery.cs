using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Competitors.Models;

namespace AlasApp.Application.Competitors.Queries.GetCompetitorPointsHistory;

public sealed record GetCompetitorPointsHistoryQuery(Guid CompetitorId, int? Year, string? CategoryId)
    : IRequest<CompetitorPointsHistoryDto>;
