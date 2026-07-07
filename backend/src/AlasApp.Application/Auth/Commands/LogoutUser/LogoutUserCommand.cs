using AlasApp.Application.Abstractions.Messaging;

namespace AlasApp.Application.Auth.Commands.LogoutUser;

public sealed record LogoutUserCommand(Guid UserId) : IRequest<bool>;
