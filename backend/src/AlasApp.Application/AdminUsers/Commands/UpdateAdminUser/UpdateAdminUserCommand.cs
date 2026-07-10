using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.AdminUsers.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.AdminUsers.Commands.UpdateAdminUser;

public sealed record UpdateAdminUserCommand(
    Guid UserId,
    AdminRole? Rol,
    AdminUserStatus? Status) : IRequest<AdminUserDto>;
