using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Auth.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Auth.Commands.LoginUser;

internal static class LoginSecurityPolicy
{
    public const int MaxFailedAttempts = 3;
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);
}

public sealed class LoginUserCommandHandler(
    IUserAccountRepository userAccountRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<LoginUserCommand, LoginResultDto>
{
    public async Task<LoginResultDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException(
                "La solicitud contiene errores de validacion.",
                [new ValidationError("email", "Email y contraseña son obligatorios.")]);
        }

        var userAccount = await userAccountRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (userAccount is null)
        {
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        try
        {
            userAccount.EnsureActive();
            userAccount.EnsureNotLocked(clock.UtcNow);
        }
        catch (DomainRuleException exception)
        {
            throw new UnauthorizedAccessException(exception.Message);
        }

        if (!passwordHasher.Verify(request.Password, userAccount.PasswordHash))
        {
            userAccount.RegisterFailedLoginAttempt(clock.UtcNow, LoginSecurityPolicy.MaxFailedAttempts, LoginSecurityPolicy.LockDuration);
            userAccount.SetUpdated(clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedAccessException(
                userAccount.LockedUntilUtc.HasValue && userAccount.LockedUntilUtc.Value > clock.UtcNow
                    ? "La cuenta está bloqueada temporalmente por múltiples intentos fallidos."
                    : "Credenciales inválidas.");
        }

        try
        {
            userAccount.RecordLogin(clock.UtcNow);
            userAccount.SetUpdated(clock.UtcNow);
        }
        catch (DomainRuleException exception)
        {
            throw new UnauthorizedAccessException(exception.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var authenticatedUser = await userAccountRepository.GetAuthenticatedUserAsync(userAccount.Id, cancellationToken)
            ?? throw new NotFoundException("No se pudo reconstruir la sesión del usuario.");

        return jwtTokenService.CreateAccessToken(authenticatedUser, userAccount.TokenVersion, request.RememberMe);
    }
}
