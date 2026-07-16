using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
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

        const string subject = "Revision requerida: token de pago en playa pendiente";

        foreach (var recipient in recipients)
        {
            try
            {
                await emailSender.SendAsync(
                    new EmailMessage(
                        recipient,
                        subject,
                        $"Se registro una nueva solicitud de token de pago en playa. TokenId: {tokenId}. InscripcionId: {inscriptionId}. Revisa el modulo de Tokens en el panel administrativo."),
                    cancellationToken);
            }
            catch
            {
                // El correo es opcional; la alerta del dashboard sigue siendo la notificacion principal.
            }
        }
    }
}
