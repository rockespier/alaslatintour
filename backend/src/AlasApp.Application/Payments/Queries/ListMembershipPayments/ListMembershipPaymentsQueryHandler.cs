using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Payments.Queries.ListMembershipPayments;

public sealed class ListMembershipPaymentsQueryHandler(IPaymentRepository paymentRepository)
    : IRequestHandler<ListMembershipPaymentsQuery, PagedResult<MembershipPaymentRowDto>>
{
    public Task<PagedResult<MembershipPaymentRowDto>> Handle(ListMembershipPaymentsQuery request, CancellationToken cancellationToken)
    {
        return paymentRepository.ListMembershipPaymentsAsync(request.Page, request.Limit, cancellationToken);
    }
}
