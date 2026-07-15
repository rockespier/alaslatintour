namespace AlasApp.Application.EventCategories.Models;

public sealed record EventCategoryUpsertItem(
    Guid CategoryId,
    int? Stars,
    decimal? CustomTariffUsd,
    int? Capacidad);
