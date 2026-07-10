using AlasApp.Domain.Enums;

namespace AlasApp.Application.AdminUsers.Models;

public sealed record RolePermissionDto(
    AdminModule Module,
    PermissionLevel Level);
