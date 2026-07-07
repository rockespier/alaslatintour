using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Payments.Commands.RejectBeachToken;

public sealed record RejectBeachTokenCommand(Guid TokenId, string Reason) : IRequest<BeachTokenAdminDto>;
