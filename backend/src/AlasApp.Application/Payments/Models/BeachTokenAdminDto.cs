using AlasApp.Domain.Enums;

namespace AlasApp.Application.Payments.Models;

public sealed record BeachTokenAdminDto(
    Guid Id,
    string CompetitorName,
    string CompetitorEmail,
    string Event,
    string Category,
    decimal AmountUsd,
    string? TokenCode,
    TokenHistoryStatus Status,
    DateTimeOffset? GeneradoAt,
    DateTimeOffset? ExpiracionAt,
    DateTimeOffset? UsadoEn);
