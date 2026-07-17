using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Emails;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Payments.Commands.RequestBeachToken;

public sealed class RequestBeachTokenCommandHandler(
    IInscriptionRepository inscriptionRepository,
    IBeachTokenRepository beachTokenRepository,
    IUserAccountRepository userAccountRepository,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<RequestBeachTokenCommand, BeachTokenPendingDto>
{
    public async Task<BeachTokenPendingDto> Handle(RequestBeachTokenCommand request, CancellationToken cancellationToken)
    {
        if (request.InscriptionId == Guid.Empty)
        {
            throw new ValidationException(
                "La solicitud contiene errores de validacion.",
                [new ValidationError("inscriptionId", "El identificador de la inscripcion es invalido.")]);
        }

        _ = await inscriptionRepository.GetEntityByIdAsync(request.InscriptionId, cancellationToken)
            ?? throw new NotFoundException("Inscripcion no encontrada.");

        if (await beachTokenRepository.HasActiveRequestAsync(request.InscriptionId, clock.UtcNow, cancellationToken))
        {
            throw new ConflictException("Ya existe una solicitud de token activa para esta inscripcion.");
        }

        try
        {
            var beachToken = BeachToken.Create(request.InscriptionId, clock.UtcNow);
            beachToken.SetCreated(clock.UtcNow);

            await beachTokenRepository.AddAsync(beachToken, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await NotifyAdminsAsync(beachToken.Id, request.InscriptionId, cancellationToken);

            return new BeachTokenPendingDto(
                beachToken.Id,
                "pending",
                "Solicitud enviada. El administrador aprobara tu token en breve.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private async Task NotifyAdminsAsync(Guid tokenId, Guid inscriptionId, CancellationToken cancellationToken)
    {
        var recipients = await userAccountRepository.ListAdminEmailsByPermissionAsync(
            AdminModule.Tokens,
            PermissionLevel.ReadOnly,
            cancellationToken);

        if (recipients.Count == 0)
        {
            return;
        }

        var token = await beachTokenRepository.GetAdminByIdAsync(tokenId, clock.UtcNow, cancellationToken);

        var subject = token is null
            ? "Revision requerida: token de pago en playa pendiente"
            : $"Revision requerida: token de pago en playa — {token.Event}";

        var textBody = token is null
            ? $"Se registro una nueva solicitud de token de pago en playa. TokenId: {tokenId}. InscripcionId: {inscriptionId}. Revisa el modulo de Tokens en el panel administrativo."
            : $"El competidor {token.CompetitorName} solicito un token de pago en playa para {token.Event} ({token.Category}). Monto: USD {token.AmountUsd:0.##}. Revisa el modulo de Tokens en el panel administrativo.";

        var htmlBody = BuildNotificationHtml(token, tokenId, inscriptionId, textBody);

        foreach (var recipient in recipients)
        {
            try
            {
                await emailSender.SendAsync(
                    new EmailMessage(recipient, subject, textBody, htmlBody),
                    cancellationToken);
            }
            catch
            {
                // El correo es opcional; la alerta del dashboard sigue siendo la notificacion principal.
            }
        }
    }

    private static string BuildNotificationHtml(BeachTokenAdminDto? token, Guid tokenId, Guid inscriptionId, string intro)
    {
        var details = token is null
            ? new List<EmailDetail>
            {
                new("Token ID", tokenId.ToString()),
                new("Inscripcion ID", inscriptionId.ToString()),
            }
            : new List<EmailDetail>
            {
                new("Competidor", token.CompetitorName),
                new("Evento", token.Event),
                new("Categoria", token.Category),
                new("Monto", $"USD {token.AmountUsd:0.##}"),
            };

        return TransactionalEmailTemplate.Render(
            "Pago en playa",
            "Token de pago en playa pendiente",
            intro,
            "Estado",
            "Pendiente de aprobacion",
            details,
            "Aprueba o rechaza esta solicitud desde el modulo de Tokens en el panel administrativo.",
            "Notificacion automatica de ALAS Latin Tour.");
    }
}
