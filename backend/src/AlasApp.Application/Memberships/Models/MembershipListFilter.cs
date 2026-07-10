using AlasApp.Domain.Enums;

namespace AlasApp.Application.Memberships.Models;

public sealed record MembershipListFilter(
    int Page,
    int Limit,
    MembershipStatus? Status);
