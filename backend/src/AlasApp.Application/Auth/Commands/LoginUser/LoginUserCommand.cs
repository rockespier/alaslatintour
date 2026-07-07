using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Auth.Models;

namespace AlasApp.Application.Auth.Commands.LoginUser;

public sealed record LoginUserCommand(string Email, string Password, bool RememberMe) : IRequest<LoginResultDto>;
