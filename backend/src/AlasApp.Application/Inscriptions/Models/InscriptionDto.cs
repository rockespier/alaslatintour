using AlasApp.Domain.Enums;

namespace AlasApp.Application.Inscriptions.Models;

public sealed record InscriptionDto(
    Guid Id,
    InscriptionCompetitorDto Competitor,
    InscriptionEventDto Event,
    InscriptionCategoryDto Category,
    InscriptionCircuitDto Circuit,
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
    DateTimeOffset InscripcionAt,
    string? Notes);
