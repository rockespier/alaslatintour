using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.AdminUsers.Models;

namespace AlasApp.Application.AdminUsers.Queries.ListAdminUsers;

public sealed record ListAdminUsersQuery() : IRequest<IReadOnlyCollection<AdminUserDto>>;
