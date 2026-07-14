using AlasApp.Domain.Enums;

namespace AlasApp.Application.Inscriptions.Models;

public sealed record InscriptionPricingContext(
    Guid EventId,
    Guid CategoryId,
    Guid CircuitId,
    bool UseCircuitTariffs,
    int EventStars,
    CategoryGender CategoryGender,
    int? CategoryCapacity,
    decimal? CustomTariffUsd,
    decimal? CircuitTariffUsd);
