using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.AdminUsers.Models;

namespace AlasApp.Application.AdminUsers.Queries.ListAdminUsers;

public sealed class ListAdminUsersQueryHandler(IUserAccountRepository userAccountRepository)
    : IRequestHandler<ListAdminUsersQuery, IReadOnlyCollection<AdminUserDto>>
{
    public Task<IReadOnlyCollection<AdminUserDto>> Handle(ListAdminUsersQuery request, CancellationToken cancellationToken)
    {
        return userAccountRepository.ListAdminUsersAsync(cancellationToken);
    }
}
