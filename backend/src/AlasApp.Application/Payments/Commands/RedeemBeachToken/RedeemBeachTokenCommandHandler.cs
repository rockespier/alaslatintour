using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Payments.Commands.RedeemBeachToken;

public sealed class RedeemBeachTokenCommandHandler(
    IInscriptionRepository inscriptionRepository,
    IBeachTokenRepository beachTokenRepository,
    IPaymentRepository paymentRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<RedeemBeachTokenCommand, BeachTokenRedeemResultDto>
{
    public async Task<BeachTokenRedeemResultDto> Handle(RedeemBeachTokenCommand request, CancellationToken cancellationToken)
    {
        if (request.InscriptionId == Guid.Empty)
        {
            throw new ValidationException(
                "La solicitud contiene errores de validacion.",
                [new ValidationError("inscriptionId", "El identificador de la inscripcion es invalido.")]);
        }

        var inscription = await inscriptionRepository.GetEntityByIdAsync(request.InscriptionId, cancellationToken)
            ?? throw new NotFoundException("Inscripcion no encontrada.");

        var token = await beachTokenRepository.GetByTokenCodeAsync(request.TokenCode.Trim().ToUpperInvariant(), cancellationToken);
        if (token is null || token.InscriptionId != request.InscriptionId)
        {
            throw new BeachTokenOperationException("token_invalid", "El token no es valido.");
        }

        if (token.Status == TokenHistoryStatus.Rechazado)
        {
            throw new BeachTokenOperationException("token_rejected", "La solicitud del token fue rechazada.", token.GeneratedAt, token.ExpirationAt);
        }

        if (token.Status == TokenHistoryStatus.Usado)
        {
            throw new BeachTokenOperationException("token_already_used", "El token ya fue utilizado.", token.GeneratedAt, token.ExpirationAt);
        }

        if (!token.IsApproved())
        {
            throw new BeachTokenOperationException("token_invalid", "El token aun no fue aprobado.");
        }

        if (token.ExpirationAt <= clock.UtcNow)
        {
            token.MarkExpired();
            token.SetUpdated(clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            throw new BeachTokenOperationException("token_expired", "El token expiro.", token.GeneratedAt, token.ExpirationAt);
        }

        if (await paymentRepository.GetEntityByInscriptionIdAsync(request.InscriptionId, cancellationToken) is not null)
        {
            throw new ConflictException("La inscripcion ya tiene un pago registrado.");
        }

        try
        {
            token.MarkUsed(clock.UtcNow);
            token.SetUpdated(clock.UtcNow);

            var payment = Payment.Create(
                inscription.Id,
                PaymentMethod.Beach,
                inscription.MontoUsd,
                token.TokenCode!,
                PaymentStatusAdmin.Pendiente,
                clock.UtcNow);

            payment.SetCreated(clock.UtcNow);
            inscription.ApplyPayment(PaymentMethod.Beach, token.TokenCode!, InscriptionStatusAdmin.Pendiente);
            inscription.SetUpdated(clock.UtcNow);

            await paymentRepository.AddAsync(payment, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var inscriptionDto = await inscriptionRepository.GetByIdAsync(inscription.Id, cancellationToken)
                ?? throw new NotFoundException("Inscripcion no encontrada despues de canjear el token.");

            return new BeachTokenRedeemResultDto(
                "success",
                $"ALAS-RBC-{clock.UtcNow.Year}-{payment.Id.ToString("N")[..4].ToUpperInvariant()}",
                inscriptionDto.Event.Nombre,
                inscriptionDto.Category.Nombre,
                inscription.MontoUsd,
                "pendiente");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }
}
