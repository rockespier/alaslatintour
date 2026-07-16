using AlasApp.Domain.Enums;

namespace AlasApp.Application.Competitors.Models;

public sealed record CompetitorInscriptionDto(
    Guid Id,
    string CompetitorId,
    string EventId,
    string EventNombre,
    string EventLugar,
    string CategoryId,
    string CategoryNombre,
    string CircuitId,
    string CircuitNombre,
    string? ShirtNumber,
    PaymentMethod PaymentMethod,
    decimal BaseAmountUsd,
    decimal? AdministrativeFeeUsd,
    decimal MontoUsd,
    InscriptionStatusAdmin EstadoAdmin,
    InscriptionStatusCompetitor EstadoCompetidor,
    string? Resultado,
    string? TransaccionId,
    bool ReglamentoAceptado,
    bool RiesgosAceptados,
    bool UsoImagenAceptado,
    DateTimeOffset InscripcionAt);
