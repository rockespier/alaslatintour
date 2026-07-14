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

        if (await paymentRepository.GetEntityByInscriptionIdAsync(request.InscriptionId, cancellationToken) is not null)
        {
            throw new ConflictException("La inscripcion ya tiene un pago registrado.");
        }

        var captureResult = await payPalGateway.CaptureOrderAsync(request.OrderId, cancellationToken);

        var payment = Payment.Create(
            request.InscriptionId,
            PaymentMethod.Paypal,
            captureResult.AmountUsd,
            captureResult.CaptureId,
            PaymentStatusAdmin.Confirmado,
            clock.UtcNow);

        payment.SetCreated(clock.UtcNow);
        inscription.ApplyPayment(PaymentMethod.Paypal, captureResult.CaptureId, InscriptionStatusAdmin.Pagado);
        inscription.SetUpdated(clock.UtcNow);

        await paymentRepository.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await paymentRepository.GetByIdAsync(payment.Id, cancellationToken)
            ?? throw new NotFoundException("Pago no encontrado despues de capturarlo.");
    }
}
