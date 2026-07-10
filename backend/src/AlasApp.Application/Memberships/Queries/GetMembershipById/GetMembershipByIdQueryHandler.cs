using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Memberships.Models;
using AlasApp.Domain.Exceptions;
using AlasApp.Application.Common;

namespace AlasApp.Application.Memberships.Queries.GetMembershipById;

public sealed class GetMembershipByIdQueryHandler(IMembershipRepository membershipRepository)
    : IRequestHandler<GetMembershipByIdQuery, MembershipDto>
{
    public async Task<MembershipDto> Handle(GetMembershipByIdQuery request, CancellationToken cancellationToken)
    {
        return await membershipRepository.GetByIdAsync(request.MembershipId, cancellationToken)
            ?? throw new NotFoundException("Membresia no encontrada.");
    }
}
