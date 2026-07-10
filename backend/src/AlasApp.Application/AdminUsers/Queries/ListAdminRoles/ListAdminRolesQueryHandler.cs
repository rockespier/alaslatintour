using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.AdminUsers.Models;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Security;

namespace AlasApp.Application.AdminUsers.Queries.ListAdminRoles;

public sealed class ListAdminRolesQueryHandler : IRequestHandler<ListAdminRolesQuery, IReadOnlyCollection<RoleDto>>
{
    public Task<IReadOnlyCollection<RoleDto>> Handle(ListAdminRolesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RoleDto> roles =
        [
            new RoleDto(AdminRole.SuperAdmin, BuildPermissions(AdminRole.SuperAdmin)),
            new RoleDto(AdminRole.Admin, BuildPermissions(AdminRole.Admin)),
            new RoleDto(AdminRole.Arbitro, BuildPermissions(AdminRole.Arbitro)),
            new RoleDto(AdminRole.Revisor, BuildPermissions(AdminRole.Revisor))
        ];

        return Task.FromResult(roles);
    }

    private static IReadOnlyCollection<RolePermissionDto> BuildPermissions(AdminRole role)
    {
        return AdminRolePermissionMatrix
            .GetPermissions(role)
            .Select(permission => new RolePermissionDto(permission.Key, permission.Value))
            .ToList();
    }
}
