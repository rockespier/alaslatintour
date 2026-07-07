using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.Auth.Commands.RequestPasswordReset;

public sealed class RequestPasswordResetCommandHandler(
    IUserAccountRepository userAccountRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IResetTokenService resetTokenService,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<RequestPasswordResetCommand, bool>
{
    public async Task<bool> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return true;
        }

        var userAccount = await userAccountRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (userAccount is null)
        {
            return true;
        }

        await passwordResetTokenRepository.ExpireActiveTokensAsync(userAccount.Id, cancellationToken);

        var rawToken = resetTokenService.GenerateToken();
        var token = PasswordResetToken.Create(
            userAccount.Id,
            resetTokenService.HashToken(rawToken),
            clock.UtcNow.AddMinutes(30));

        token.SetCreated(clock.UtcNow);

        await passwordResetTokenRepository.AddAsync(token, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
