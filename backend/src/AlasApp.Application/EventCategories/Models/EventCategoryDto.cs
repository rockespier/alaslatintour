namespace AlasApp.Application.EventCategories.Models;

public sealed record EventCategoryDto(
    Guid CategoryId,
    string CategoryName,
    decimal? CustomTariffUsd,
    decimal? CustomTariffCop,
    int? Capacidad,
    decimal EffectiveTariffUsd,
    decimal EffectiveTariffCop,
    int EnrolledCount);
