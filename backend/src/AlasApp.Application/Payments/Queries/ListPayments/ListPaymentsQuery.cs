using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Common;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Payments.Queries.ListPayments;

public sealed record ListPaymentsQuery(PaymentListFilter Filter) : IRequest<PagedResult<PaymentDto>>;
