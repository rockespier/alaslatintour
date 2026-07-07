using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;

namespace AlasApp.Application.Competitors.Queries.GetCompetitorInscriptions;

public sealed record GetCompetitorInscriptionsQuery(Guid CompetitorId, string? Status) : IRequest<PagedResult<CompetitorInscriptionDto>>;
