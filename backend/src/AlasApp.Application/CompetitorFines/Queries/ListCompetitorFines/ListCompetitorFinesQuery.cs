using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.CompetitorFines.Models;

namespace AlasApp.Application.CompetitorFines.Queries.ListCompetitorFines;

public sealed record ListCompetitorFinesQuery(Guid CompetitorId) : IRequest<IReadOnlyCollection<CompetitorFineDto>>;
