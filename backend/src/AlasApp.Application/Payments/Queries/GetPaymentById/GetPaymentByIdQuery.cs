using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Payments.Queries.GetPaymentById;

public sealed record GetPaymentByIdQuery(Guid PaymentId) : IRequest<PaymentDto>;
