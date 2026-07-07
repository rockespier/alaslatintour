using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Payments.Commands.UpdatePayment;

public sealed record UpdatePaymentCommand(
    Guid PaymentId,
    PaymentStatusAdmin Status,
    string? Notes) : IRequest<PaymentDto>;
