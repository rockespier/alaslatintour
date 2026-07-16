using AlasApp.Application.Abstractions.Messaging;

namespace AlasApp.Application.Auth.Commands.ChangeUserPassword;

public sealed record ChangeUserPasswordCommand(Guid UserId, string NewPassword) : IRequest<bool>;
