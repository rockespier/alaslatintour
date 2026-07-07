using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Payments.Commands.RequestBeachToken;

public sealed record RequestBeachTokenCommand(Guid InscriptionId) : IRequest<BeachTokenPendingDto>;
