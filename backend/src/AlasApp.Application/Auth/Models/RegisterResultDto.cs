using AlasApp.Domain.Enums;

namespace AlasApp.Application.Auth.Models;

public sealed record RegisterResultDto(
    Guid Id,
    string Email,
    UserType Tipo,
    LicenseStatus? LicenseStatus,
    string Message);
