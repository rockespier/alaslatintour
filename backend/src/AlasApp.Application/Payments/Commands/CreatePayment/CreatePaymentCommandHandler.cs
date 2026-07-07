using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Payments.Commands.CreatePayment;

public sealed class CreatePaymentCommandHandler(
    IInscriptionRepository inscriptionRepository,
    IPaymentRepository paymentRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<CreatePaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        var inscription = await inscriptionRepository.GetEntityByIdAsync(request.InscriptionId, cancellationToken)
            ?? throw new NotFoundException("Inscripcion no encontrada.");

        if (await paymentRepository.ExistsByTransactionIdAsync(request.TransactionId, cancellationToken))
        {
            throw new ConflictException("Ya existe un pago con la misma transaccion.");
        }

        if (await paymentRepository.GetEntityByInscriptionIdAsync(request.InscriptionId, cancellationToken) is not null)
        {
            throw new ConflictException("La inscripcion ya tiene un pago registrado.");
        }

        if (decimal.Round(request.AmountUsd, 2) != decimal.Round(inscription.MontoUsd, 2))
        {
            throw new ValidationException(
                "La solicitud contiene errores de validacion.",
                [new ValidationError("amountUsd", "El monto del pago debe coincidir con el monto de la inscripcion.")]);
        }

        try
        {
            var payment = Payment.Create(
                request.InscriptionId,
                request.Method,
                request.AmountUsd,
                request.TransactionId,
                PaymentStatusAdmin.Confirmado,
                clock.UtcNow);

            payment.SetCreated(clock.UtcNow);
            inscription.ApplyPayment(request.Method, request.TransactionId, InscriptionStatusAdmin.Pagado);
            inscription.SetUpdated(clock.UtcNow);

            await paymentRepository.AddAsync(payment, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await paymentRepository.GetByIdAsync(payment.Id, cancellationToken)
                ?? throw new NotFoundException("Pago no encontrado despues de crearlo.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private static void Validate(CreatePaymentCommand request)
    {
        var errors = new List<ValidationError>();

        if (request.InscriptionId == Guid.Empty)
        {
            errors.Add(new ValidationError("inscriptionId", "El identificador de la inscripcion es invalido."));
        }

        if (request.AmountUsd < 0)
        {
            errors.Add(new ValidationError("amountUsd", "El monto del pago no puede ser negativo."));
        }

        if (string.IsNullOrWhiteSpace(request.TransactionId))
        {
            errors.Add(new ValidationError("transactionId", "La transaccion del pago es obligatoria."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }
    }
}
