using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.AdminUsers.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.AdminUsers.Commands.CreateAdminUser;

public sealed record CreateAdminUserCommand(
    string Nombre,
    string Apellido,
    string Email,
    AdminRole Rol,
    bool SendInvitationEmail) : IRequest<AdminUserDto>;
