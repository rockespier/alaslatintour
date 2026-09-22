using AlasApp.Domain.Enums;

namespace AlasApp.Application.Payments.Models;

public sealed record MembershipPaymentRowDto(
    Guid PaymentId,
    string CompetitorName,
    DateTimeOffset PaymentDate,
    MembershipPlanOption MembershipPlan,
    string? EventName,
    string CircuitName,
    PaymentMethod Method,
    decimal AmountUsd);
