using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Payments.Queries.ListPayments;

public sealed class ListPaymentsQueryHandler(IPaymentRepository paymentRepository)
    : IRequestHandler<ListPaymentsQuery, PagedResult<PaymentDto>>
{
    public Task<PagedResult<PaymentDto>> Handle(ListPaymentsQuery request, CancellationToken cancellationToken)
    {
        return paymentRepository.ListAsync(request.Filter, cancellationToken);
    }
}
