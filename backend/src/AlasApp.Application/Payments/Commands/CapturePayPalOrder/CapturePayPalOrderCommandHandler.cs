using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Payments.Commands.CapturePayPalOrder;

public sealed class CapturePayPalOrderCommandHandler(
    IInscriptionRepository inscriptionRepository,
    IPaymentRepository paymentRepository,
    IPayPalGateway payPalGateway,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<CapturePayPalOrderCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(CapturePayPalOrderCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            throw new ValidationException("Solicitud invalida.", [new ValidationError("orderId", "El identificador de la orden PayPal es obligatorio.")]);
        }

        var inscription = await inscriptionRepository.GetEntityByIdAsync(request.InscriptionId, cancellationToken)
            ?? throw new NotFoundException("Inscripcion no encontrada.");

        if (!string.Equals(inscription.PayPalOrderId, request.OrderId, StringComparison.Ordinal))
        {
            throw new ConflictException("La orden de PayPal no corresponde a esta inscripcion.");
        }

        var group = await inscriptionRepository.ListEntitiesByGroupIdAsync(inscription.InscriptionGroupId, cancellationToken);

        foreach (var item in group)
        {
            if (await paymentRepository.GetEntityByInscriptionIdAsync(item.Id, cancellationToken) is not null)
            {
                throw new ConflictException("La inscripcion ya tiene un pago registrado.");
            }
        }

        var captureResult = await payPalGateway.CaptureOrderAsync(request.OrderId, cancellationToken);

        var expectedAmount = decimal.Round(group.Sum(x => x.MontoUsd), 2);
        if (!string.Equals(captureResult.OrderId, request.OrderId, StringComparison.Ordinal)
            || decimal.Round(captureResult.AmountUsd, 2) != expectedAmount)
        {
            throw new ConflictException("El monto capturado por PayPal no coincide con el total esperado de la inscripcion.");
        }

        var payment = Payment.Create(
            request.InscriptionId,
            PaymentMethod.Paypal,
            group.Sum(x => x.MontoUsd),
            captureResult.CaptureId,
            PaymentStatusAdmin.Confirmado,
            clock.UtcNow);

        payment.SetCreated(clock.UtcNow);
        foreach (var item in group)
        {
            item.ApplyPayment(PaymentMethod.Paypal, captureResult.CaptureId, InscriptionStatusAdmin.Pagado);
            item.SetUpdated(clock.UtcNow);
        }

        await paymentRepository.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await paymentRepository.GetByIdAsync(payment.Id, cancellationToken)
            ?? throw new NotFoundException("Pago no encontrado despues de capturarlo.");
    }
}
