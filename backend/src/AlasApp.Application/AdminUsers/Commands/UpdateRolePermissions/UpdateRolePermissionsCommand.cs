using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.AdminUsers.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.AdminUsers.Commands.UpdateRolePermissions;

public sealed record UpdateRolePermissionsCommand(
    AdminRole Role,
    IReadOnlyDictionary<AdminModule, PermissionLevel> Permissions) : IRequest<RoleDto>;
