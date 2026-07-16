using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.CompetitorFines.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.CompetitorFines.Commands.UpdateCompetitorFine;

public sealed record UpdateCompetitorFineCommand(
    Guid FineId,
    decimal AmountUsd,
    string Reason,
    string? Notes,
    CompetitorFineStatus Status) : IRequest<CompetitorFineDto>;
