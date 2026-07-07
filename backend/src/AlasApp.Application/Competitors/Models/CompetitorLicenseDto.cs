using AlasApp.Domain.Enums;

namespace AlasApp.Application.Competitors.Models;

public sealed record CompetitorLicenseDto(
    string Number,
    string NumberLong,
    LicenseStatus Status,
    DateTimeOffset ExpirationDate,
    IReadOnlyCollection<string> EnabledCategories);
