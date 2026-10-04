using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Emails;
using AlasApp.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AlasApp.Application.Auth.Commands.RequestPasswordReset;

public sealed class RequestPasswordResetCommandHandler(
    IUserAccountRepository userAccountRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IResetTokenService resetTokenService,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<RequestPasswordResetCommandHandler> logger)
    : IRequestHandler<RequestPasswordResetCommand, bool>
{
    public async Task<bool> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            logger.LogInformation("Solicitud de recuperacion de contrasena recibida sin email.");
            return true;
        }

        var userAccount = await userAccountRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (userAccount is null)
        {
            logger.LogInformation("Solicitud de recuperacion de contrasena recibida para un email no registrado.");
            return true;
        }

        logger.LogInformation("Solicitud de recuperacion de contrasena recibida para el usuario {UserId}.", userAccount.Id);

        await passwordResetTokenRepository.ExpireActiveTokensAsync(userAccount.Id, cancellationToken);

        var rawToken = resetTokenService.GenerateToken();
        var token = PasswordResetToken.Create(
            userAccount.Id,
            resetTokenService.HashToken(rawToken),
            clock.UtcNow.AddMinutes(30));

        token.SetCreated(clock.UtcNow);

        await passwordResetTokenRepository.AddAsync(token, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var email = CompetitorEmails.PasswordReset(EmailLanguage.From(userAccount.IdiomaPreferido), rawToken);
            await emailSender.SendAsync(
                new EmailMessage(userAccount.Email, email.Subject, email.Text, email.Html),
                cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("No se pudo enviar el correo de recuperacion de contrasena para el usuario {UserId}.", userAccount.Id);
            return true;
        }

        return true;
    }
}
