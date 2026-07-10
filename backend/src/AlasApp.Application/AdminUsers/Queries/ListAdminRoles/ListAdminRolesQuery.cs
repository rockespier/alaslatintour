using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.AdminUsers.Models;

namespace AlasApp.Application.AdminUsers.Queries.ListAdminRoles;

public sealed record ListAdminRolesQuery() : IRequest<IReadOnlyCollection<RoleDto>>;
