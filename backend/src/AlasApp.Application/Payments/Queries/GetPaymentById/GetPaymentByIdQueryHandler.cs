using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Payments.Queries.GetPaymentById;

public sealed class GetPaymentByIdQueryHandler(IPaymentRepository paymentRepository)
    : IRequestHandler<GetPaymentByIdQuery, PaymentDto>
{
    public async Task<PaymentDto> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        return await paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new NotFoundException("Pago no encontrado.");
    }
}
