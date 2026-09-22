using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Payments.Commands.InitiatePayPalOrder;

public sealed class InitiatePayPalOrderCommandHandler(
    IInscriptionRepository inscriptionRepository,
    IPaymentRepository paymentRepository,
    IPayPalGateway payPalGateway,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<InitiatePayPalOrderCommand, PayPalOrderDto>
{
    public async Task<PayPalOrderDto> Handle(InitiatePayPalOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.InscriptionId == Guid.Empty)
        {
            throw new ValidationException("Solicitud invalida.", [new ValidationError("inscriptionId", "El identificador de la inscripcion es invalido.")]);
        }

        var inscription = await inscriptionRepository.GetEntityByIdAsync(request.InscriptionId, cancellationToken)
            ?? throw new NotFoundException("Inscripcion no encontrada.");
        var group = await inscriptionRepository.ListEntitiesByGroupIdAsync(inscription.InscriptionGroupId, cancellationToken);

        if (inscription.PaymentMethod != PaymentMethod.Paypal)
        {
            throw new ValidationException(
                "El metodo de pago de la inscripcion no es PayPal.",
                [new ValidationError("inscriptionId", "El metodo de pago de la inscripcion no es PayPal.")]);
        }

        foreach (var item in group)
        {
            if (await paymentRepository.GetEntityByInscriptionIdAsync(item.Id, cancellationToken) is not null)
            {
                throw new ConflictException("La inscripcion ya tiene un pago registrado.");
            }
        }

        var result = await payPalGateway.CreateOrderAsync(
            request.InscriptionId,
            group.Sum(x => x.MontoUsd),
            request.ReturnUrl,
            request.CancelUrl,
            cancellationToken);

        inscription.AssignPayPalOrder(result.OrderId);
        inscription.SetUpdated(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PayPalOrderDto(result.OrderId, result.ApprovalUrl, group.Sum(x => x.MontoUsd));
    }
}
