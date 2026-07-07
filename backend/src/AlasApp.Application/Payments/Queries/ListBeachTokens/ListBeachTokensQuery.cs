using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Payments.Queries.ListBeachTokens;

public sealed record ListBeachTokensQuery(int Page, int Limit, TokenHistoryStatus? Status) : IRequest<BeachTokenAdminListDto>;
