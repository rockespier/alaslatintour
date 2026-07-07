using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Competitors.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Competitors.Commands.UpdateCompetitorLicense;

public sealed record UpdateCompetitorLicenseCommand(
    Guid CompetitorId,
    LicenseStatus Status,
    string LicenseNumber,
    DateTimeOffset ExpirationDate,
    IReadOnlyCollection<string> EnabledCategories) : IRequest<CompetitorDto>;
