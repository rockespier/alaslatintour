using AlasApp.Domain.Enums;

namespace AlasApp.Application.Competitors.Models;

public sealed record CompetitorListFilter(
    int Page,
    int Limit,
    string? Country,
    string? CategoryId,
    LicenseStatus? LicenseStatus,
    string? Search);
