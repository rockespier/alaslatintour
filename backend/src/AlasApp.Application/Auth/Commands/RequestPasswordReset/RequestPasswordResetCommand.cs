using AlasApp.Application.Abstractions.Messaging;

namespace AlasApp.Application.Auth.Commands.RequestPasswordReset;

public sealed record RequestPasswordResetCommand(string Email) : IRequest<bool>;
