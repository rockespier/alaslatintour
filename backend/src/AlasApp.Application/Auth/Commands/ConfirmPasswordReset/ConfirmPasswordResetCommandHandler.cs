using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Auth.Commands.ConfirmPasswordReset;

public sealed class ConfirmPasswordResetCommandHandler(
    IUserAccountRepository userAccountRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IResetTokenService resetTokenService,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IClock clock)
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
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }

        var tokenHash = resetTokenService.HashToken(request.Token);
        var resetToken = await passwordResetTokenRepository.GetActiveByHashAsync(tokenHash, cancellationToken);

        if (resetToken is null || resetToken.IsExpired(clock.UtcNow))
        {
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

        await passwordResetTokenRepository.ExpireActiveTokensAsync(userAccount.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
