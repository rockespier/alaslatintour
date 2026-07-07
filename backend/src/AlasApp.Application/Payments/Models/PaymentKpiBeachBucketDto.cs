namespace AlasApp.Application.Payments.Models;

public sealed record PaymentKpiBeachBucketDto(
    decimal AmountUsd,
    int Count,
    int PendingCount);
