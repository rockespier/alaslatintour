using AlasApp.Domain.Enums;

namespace AlasApp.Application.AdminUsers.Models;

public sealed record AdminUserDto(
    Guid Id,
    string Initials,
    string FullName,
    string Email,
    AdminRole Role,
    AdminUserStatus Status,
    DateTimeOffset? LastSession,
    DateTimeOffset CreatedAt,
    bool IsLocked,
    DateTimeOffset? LockedUntil,
    int FailedLoginAttempts);
