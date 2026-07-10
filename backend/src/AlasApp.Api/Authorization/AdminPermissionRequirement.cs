using AlasApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace AlasApp.Api.Authorization;

public sealed class AdminPermissionRequirement(AdminModule module, PermissionLevel permissionLevel) : IAuthorizationRequirement
{
    public AdminModule Module { get; } = module;

    public PermissionLevel PermissionLevel { get; } = permissionLevel;
}
