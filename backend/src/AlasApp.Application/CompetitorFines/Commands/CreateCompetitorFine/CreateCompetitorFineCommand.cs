using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.CompetitorFines.Models;

namespace AlasApp.Application.CompetitorFines.Commands.CreateCompetitorFine;

public sealed record CreateCompetitorFineCommand(
    Guid CompetitorId,
    decimal AmountUsd,
    string Reason,
    string? Notes,
    Guid CreatedByUserId) : IRequest<CompetitorFineDto>;
