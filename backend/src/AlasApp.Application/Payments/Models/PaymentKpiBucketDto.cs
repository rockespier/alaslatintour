namespace AlasApp.Application.Payments.Models;

public sealed record PaymentKpiBucketDto(
    decimal AmountUsd,
    int Count);
