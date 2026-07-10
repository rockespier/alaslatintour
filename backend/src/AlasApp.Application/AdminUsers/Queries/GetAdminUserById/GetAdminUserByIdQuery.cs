using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.AdminUsers.Models;

namespace AlasApp.Application.AdminUsers.Queries.GetAdminUserById;

public sealed record GetAdminUserByIdQuery(Guid UserId) : IRequest<AdminUserDto>;
