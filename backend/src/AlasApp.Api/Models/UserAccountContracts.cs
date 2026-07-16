using AlasApp.Domain.Enums;

namespace AlasApp.Api.Models;

public sealed class PasswordChangeRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

public class CompetitorFineRequest
{
    public decimal AmountUsd { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

public sealed class CompetitorFineUpdateRequest : CompetitorFineRequest
{
    public CompetitorFineStatus Status { get; set; }
}

public sealed record CompetitorFineResponse(
    string Id,
    string CompetitorId,
    decimal AmountUsd,
    string Reason,
    string Notes,
    string Status,
    string CreatedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
