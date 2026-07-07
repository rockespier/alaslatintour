using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Payments.Commands.RedeemBeachToken;

public sealed record RedeemBeachTokenCommand(Guid InscriptionId, string TokenCode) : IRequest<BeachTokenRedeemResultDto>;
