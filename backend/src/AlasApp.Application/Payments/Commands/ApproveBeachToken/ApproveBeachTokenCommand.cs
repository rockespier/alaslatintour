using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Payments.Commands.ApproveBeachToken;

public sealed record ApproveBeachTokenCommand(Guid TokenId) : IRequest<BeachTokenAdminDto>;
