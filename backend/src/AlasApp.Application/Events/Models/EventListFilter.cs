using AlasApp.Domain.Enums;

namespace AlasApp.Application.Events.Models;

public sealed record EventListFilter(
    int Page,
    int Limit,
    Guid? CircuitId,
    EventStatusPublic? Status,
    string? Country,
    int? Year,
    int? Stars);
