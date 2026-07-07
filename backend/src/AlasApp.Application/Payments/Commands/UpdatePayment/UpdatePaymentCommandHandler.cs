using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Payments.Commands.UpdatePayment;

public sealed class UpdatePaymentCommandHandler(
    IPaymentRepository paymentRepository,
    IInscriptionRepository inscriptionRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<UpdatePaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(UpdatePaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetEntityByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new NotFoundException("Pago no encontrado.");

        var inscription = await inscriptionRepository.GetEntityByIdAsync(payment.InscriptionId, cancellationToken)
            ?? throw new NotFoundException("Inscripcion no encontrada.");

        try
        {
            payment.Update(request.Status, request.Notes);
            payment.SetUpdated(clock.UtcNow);

            inscription.ApplyPayment(
                payment.Method,
                payment.TransactionId,
                request.Status == PaymentStatusAdmin.Confirmado
                    ? InscriptionStatusAdmin.Pagado
                    : InscriptionStatusAdmin.Pendiente);
            inscription.SetUpdated(clock.UtcNow);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await paymentRepository.GetByIdAsync(payment.Id, cancellationToken)
                ?? throw new NotFoundException("Pago no encontrado despues de actualizarlo.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }
}
