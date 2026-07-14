using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Payments.Commands.CapturePayPalOrder;

public sealed record CapturePayPalOrderCommand(Guid InscriptionId, string OrderId) : IRequest<PaymentDto>;
