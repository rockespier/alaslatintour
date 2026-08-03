using AlasApp.Application.Abstractions.Services;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AlasApp.Api.Authorization;

public sealed class AdminPermissionAuthorizationHandler(IAdminRolePermissionProvider permissionProvider)
    : AuthorizationHandler<AdminPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminPermissionRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return;
        }

        var roleValue = context.User.FindFirstValue("admin_role") ?? context.User.FindFirstValue(ClaimTypes.Role);
        if (!Enum.TryParse<AdminRole>(roleValue, out var role))
        {
            return;
        }

        var actualLevel = await permissionProvider.GetPermissionAsync(role, requirement.Module, CancellationToken.None);
        if (AdminRolePermissionMatrix.Satisfies(actualLevel, requirement.PermissionLevel))
        {
            context.Succeed(requirement);
        }
    }
}
