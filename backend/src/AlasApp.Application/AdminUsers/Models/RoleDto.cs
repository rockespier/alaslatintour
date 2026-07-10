using AlasApp.Domain.Enums;

namespace AlasApp.Application.AdminUsers.Models;

public sealed record RoleDto(
    AdminRole Name,
    IReadOnlyCollection<RolePermissionDto> Permissions);
