using AlasApp.Domain.Enums;

namespace AlasApp.Application.CompetitorFines.Models;

public sealed record CompetitorFineDto(
    Guid Id,
    Guid CompetitorId,
    decimal AmountUsd,
    string Reason,
    string Notes,
    CompetitorFineStatus Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
