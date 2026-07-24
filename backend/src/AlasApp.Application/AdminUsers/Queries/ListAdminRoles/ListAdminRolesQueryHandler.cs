using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminUsers.Models;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Security;

namespace AlasApp.Application.AdminUsers.Queries.ListAdminRoles;

public sealed class ListAdminRolesQueryHandler(IAdminRolePermissionProvider provider)
    : IRequestHandler<ListAdminRolesQuery, IReadOnlyCollection<RoleDto>>
{
    private static readonly AdminRole[] AllRoles =
    [
        AdminRole.SuperAdmin,
        AdminRole.Admin,
        AdminRole.Arbitro,
        AdminRole.Revisor
    ];

    public async Task<IReadOnlyCollection<RoleDto>> Handle(ListAdminRolesQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await provider.GetSnapshotAsync(cancellationToken);

        return AllRoles
            .Select(role => new RoleDto(role, BuildPermissions(role, snapshot)))
            .ToList();
    }

    private static IReadOnlyCollection<RolePermissionDto> BuildPermissions(
        AdminRole role,
        IReadOnlyDictionary<(AdminRole Role, AdminModule Module), PermissionLevel> snapshot)
    {
        return AdminRolePermissionMatrix.Modules
            .Select(module => new RolePermissionDto(module, snapshot[(role, module)]))
            .ToList();
    }
}
