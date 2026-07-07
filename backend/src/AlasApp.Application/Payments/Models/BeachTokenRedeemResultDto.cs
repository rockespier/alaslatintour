namespace AlasApp.Application.Payments.Models;

public sealed record BeachTokenRedeemResultDto(
    string Status,
    string Reference,
    string Evento,
    string Categoria,
    decimal MontoUsd,
    string FinancialStatus);
