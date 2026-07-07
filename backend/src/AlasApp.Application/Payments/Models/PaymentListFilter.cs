using AlasApp.Domain.Enums;

namespace AlasApp.Application.Payments.Models;

public sealed record PaymentListFilter(
    int Page,
    int Limit,
    PaymentMethod? Method,
    PaymentStatusAdmin? Status,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate);
