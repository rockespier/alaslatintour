using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Payments.Commands.InitiatePayPalOrder;

public sealed record InitiatePayPalOrderCommand(
    Guid InscriptionId,
    string ReturnUrl,
    string CancelUrl) : IRequest<PayPalOrderDto>;
