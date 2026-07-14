using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace AlasApp.Application.Auth.Commands.ConfirmPasswordReset;

public sealed class ConfirmPasswordResetCommandHandler(
    IUserAccountRepository userAccountRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IResetTokenService resetTokenService,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<ConfirmPasswordResetCommandHandler> logger)
    : IRequestHandler<ConfirmPasswordResetCommand, bool>
{
    public async Task<bool> Handle(ConfirmPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            errors.Add(new ValidationError("token", "El token es obligatorio."));
        }

        if (!PasswordPolicy.IsValid(request.NewPassword))
        {
            errors.Add(new ValidationError("newPassword", PasswordPolicy.Message));
        }

        if (errors.Count > 0)
        {
            logger.LogInformation(
                "Confirmacion de recuperacion rechazada por validacion. Fields: {Fields}.",
                string.Join(", ", errors.Select(x => x.Field)));
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }

        var normalizedToken = NormalizeToken(request.Token);
        var tokenHash = resetTokenService.HashToken(normalizedToken);
        var resetToken = await passwordResetTokenRepository.GetActiveByHashAsync(tokenHash, cancellationToken);

        if (resetToken is null)
        {
            logger.LogInformation("Confirmacion de recuperacion rechazada porque el token no existe o ya fue usado.");
            throw new ValidationException(
                "El token de recuperación es inválido o expiró.",
                [new ValidationError("token", "El token de recuperación es inválido o expiró.")]);
        }

        if (resetToken.IsExpired(clock.UtcNow))
        {
            logger.LogInformation("Confirmacion de recuperacion rechazada porque el token {TokenId} expiro.", resetToken.Id);
            throw new ValidationException(
                "El token de recuperación es inválido o expiró.",
                [new ValidationError("token", "El token de recuperación es inválido o expiró.")]);
        }

        var userAccount = await userAccountRepository.GetByIdAsync(resetToken.UserAccountId, cancellationToken)
            ?? throw new NotFoundException("No se encontró el usuario asociado al token.");

        try
        {
            userAccount.ChangePassword(passwordHasher.Hash(request.NewPassword), clock.UtcNow);
            userAccount.SetUpdated(clock.UtcNow);
            resetToken.MarkUsed(clock.UtcNow);
            resetToken.SetUpdated(clock.UtcNow);
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Contrasena restablecida correctamente para el usuario {UserId}.", userAccount.Id);
        return true;
    }

    private static string NormalizeToken(string token)
    {
        return string.Concat(token.Where(x => !char.IsWhiteSpace(x)));
    }
}
