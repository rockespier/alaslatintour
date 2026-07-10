using AlasApp.Application.Abstractions.Messaging;

namespace AlasApp.Application.AdminUsers.Commands.DeleteAdminUser;

public sealed record DeleteAdminUserCommand(Guid UserId, Guid? CurrentUserId) : IRequest<bool>;
