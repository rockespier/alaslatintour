using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Competitors.Models;

namespace AlasApp.Application.Competitors.Queries.GetCompetitorById;

public sealed record GetCompetitorByIdQuery(Guid CompetitorId) : IRequest<CompetitorDto>;
