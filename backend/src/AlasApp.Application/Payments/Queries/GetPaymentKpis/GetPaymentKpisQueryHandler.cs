using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Payments.Queries.GetPaymentKpis;

public sealed class GetPaymentKpisQueryHandler(
    IPaymentRepository paymentRepository,
    IClock clock)
    : IRequestHandler<GetPaymentKpisQuery, PaymentKpiDto>
{
    public Task<PaymentKpiDto> Handle(GetPaymentKpisQuery request, CancellationToken cancellationToken)
    {
        return paymentRepository.GetKpisAsync(clock.UtcNow, cancellationToken);
    }
}
