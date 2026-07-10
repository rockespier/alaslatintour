using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Memberships.Models;

namespace AlasApp.Application.Memberships.Queries.ListMemberships;

public sealed class ListMembershipsQueryHandler(IMembershipRepository membershipRepository)
    : IRequestHandler<ListMembershipsQuery, PagedResult<MembershipDto>>
{
    public Task<PagedResult<MembershipDto>> Handle(ListMembershipsQuery request, CancellationToken cancellationToken)
    {
        return membershipRepository.ListAsync(request.Filter, cancellationToken);
    }
}
