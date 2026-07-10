using AlasApp.Application.Common;
using AlasApp.Application.Memberships.Models;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.Abstractions.Persistence;

public interface IMembershipRepository
{
    Task<PagedResult<MembershipDto>> ListAsync(MembershipListFilter filter, CancellationToken cancellationToken);

    Task<MembershipDto?> GetByIdAsync(Guid membershipId, CancellationToken cancellationToken);

    Task<Membership?> GetEntityByIdAsync(Guid membershipId, CancellationToken cancellationToken);

    Task AddAsync(Membership membership, CancellationToken cancellationToken);

    void Remove(Membership membership);
}
