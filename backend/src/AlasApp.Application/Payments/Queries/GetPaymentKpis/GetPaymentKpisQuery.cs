using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Payments.Queries.GetPaymentKpis;

public sealed record GetPaymentKpisQuery() : IRequest<PaymentKpiDto>;
