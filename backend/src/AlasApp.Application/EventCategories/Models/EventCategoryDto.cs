using AlasApp.Domain.Enums;

namespace AlasApp.Application.EventCategories.Models;

/// <summary>
/// Nivel de estrellas propio de esta categoria en el evento (override). Null = hereda Event.Stars.
/// No confundir con el nivel efectivamente usado para resolver la tarifa, que ya viene reflejado en
/// EffectiveTariffUsd.
/// </summary>
public sealed record EventCategoryDto(
    Guid CategoryId,
    string CategoryName,
    CategoryGender Gender,
    int? Stars,
    decimal? CustomTariffUsd,
    int? Capacidad,
    decimal EffectiveTariffUsd,
    int EnrolledCount);
