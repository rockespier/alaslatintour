namespace AlasApp.Application.CategoryTariffs.Models;

public sealed record CategoryTariffDto(int StarLevel, decimal Usd, decimal Cop, bool Active);
