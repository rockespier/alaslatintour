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
            await emailSender.SendAsync(
                new EmailMessage(
                    userAccount.Email,
                    "Recuperacion de contrasena ALAS Global Tour",
                    BuildPasswordResetText(rawToken),
                    BuildPasswordResetHtml(rawToken)),
                cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("No se pudo enviar el correo de recuperacion de contrasena para el usuario {UserId}.", userAccount.Id);
            return true;
        }

        return true;
    }

    private static string BuildPasswordResetText(string token)
    {
        return $"""
            Recibimos una solicitud para restablecer tu contrasena de ALAS Global Tour.

            Usa este token para confirmar la nueva contrasena:
            {token}

            El token expira en 30 minutos. Si no solicitaste este cambio, ignora este correo.
            """;
    }

    private static string BuildPasswordResetHtml(string token)
    {
        return TransactionalEmailTemplate.Render(
            "Seguridad",
            "Restablece tu contrasena",
            "Recibimos una solicitud para restablecer tu contrasena de ALAS Global Tour. Ingresa este token en la pantalla de recuperacion para continuar.",
            "Token de recuperacion",
            token,
            [
                new EmailDetail("Validez", "30 minutos"),
                new EmailDetail("Uso", "Un solo intento de recuperacion")
            ],
            "Si no solicitaste este cambio, puedes ignorar este correo. Tu contrasena actual seguira siendo valida.",
            "Este mensaje fue enviado automaticamente por ALAS Global Tour.");
    }
}
