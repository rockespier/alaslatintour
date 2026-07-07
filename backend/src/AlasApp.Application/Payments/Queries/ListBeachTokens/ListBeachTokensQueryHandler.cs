using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Payments.Queries.ListBeachTokens;

public sealed class ListBeachTokensQueryHandler(
    IBeachTokenRepository beachTokenRepository,
    IClock clock)
    : IRequestHandler<ListBeachTokensQuery, BeachTokenAdminListDto>
{
    public Task<BeachTokenAdminListDto> Handle(ListBeachTokensQuery request, CancellationToken cancellationToken)
    {
        return beachTokenRepository.ListAdminAsync(request.Page, request.Limit, request.Status, clock.UtcNow, cancellationToken);
    }
}
