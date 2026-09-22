using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Common;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Payments.Queries.ListMembershipPayments;

public sealed record ListMembershipPaymentsQuery(int Page, int Limit) : IRequest<PagedResult<MembershipPaymentRowDto>>;
