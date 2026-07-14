namespace AlasApp.Application.Payments.Models;

public sealed record PayPalOrderDto(string OrderId, string ApprovalUrl, decimal AmountUsd);
