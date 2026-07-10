using AlasApp.Domain.Enums;
using AlasApp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AlasApp.Api.Authorization;

public sealed class AdminPermissionAuthorizationHandler : AuthorizationHandler<AdminPermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminPermissionRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Task.CompletedTask;
        }

        var roleValue = context.User.FindFirstValue("admin_role") ?? context.User.FindFirstValue(ClaimTypes.Role);
        if (!Enum.TryParse<AdminRole>(roleValue, out var role))
        {
            return Task.CompletedTask;
        }

        if (AdminRolePermissionMatrix.HasPermission(role, requirement.Module, requirement.PermissionLevel))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
