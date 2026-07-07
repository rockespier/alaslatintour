using AlasApp.Application.Abstractions.Messaging;

namespace AlasApp.Application.Auth.Commands.ConfirmPasswordReset;

public sealed record ConfirmPasswordResetCommand(string Token, string NewPassword) : IRequest<bool>;
