using AlasApp.Domain.Enums;

namespace AlasApp.Application.Auth.Models;

public sealed record AuthenticatedUserDto(
    Guid Id,
    string Email,
    string FullName,
    UserType Tipo,
    AdminRole? AdminRole,
    Guid? CompetitorId);
