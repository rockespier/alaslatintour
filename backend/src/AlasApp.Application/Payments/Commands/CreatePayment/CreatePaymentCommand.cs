using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Payments.Commands.CreatePayment;

public sealed record CreatePaymentCommand(
    Guid InscriptionId,
    PaymentMethod Method,
    decimal AmountUsd,
    string TransactionId) : IRequest<PaymentDto>;
