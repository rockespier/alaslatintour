using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.EventResults.Models;

namespace AlasApp.Application.EventResults.Queries.GetPrizeDistribution;

public sealed record GetPrizeDistributionQuery(Guid EventId) : IRequest<PrizeDistributionDto>;
