using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;

namespace AlasApp.Application.Competitors.Queries.ListCompetitors;

public sealed record ListCompetitorsQuery(CompetitorListFilter Filter) : IRequest<PagedResult<CompetitorDto>>;
