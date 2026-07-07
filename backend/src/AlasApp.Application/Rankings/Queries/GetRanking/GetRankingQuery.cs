using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Rankings.Models;

namespace AlasApp.Application.Rankings.Queries.GetRanking;

public sealed record GetRankingQuery(Guid CategoryId, int? Year, int? Page, int? Limit) : IRequest<RankingDto>;
