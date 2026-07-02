namespace AlasApp.Application.EventCategories.Models;

public sealed record EventCategoryUpsertItem(
    Guid CategoryId,
    decimal? CustomTariffUsd,
    decimal? CustomTariffCop,
    int? Capacidad);
