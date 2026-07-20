using AlasApp.Domain.Enums;

namespace AlasApp.Application.Inscriptions.Models;

public sealed record AdminInscriptionRowDto(
    Guid Id,
    Guid CompetitorId,
    string SequentialNumber,
    string FullName,
    string Country,
    string? Ranking2025,
    string? Ranking2026,
    string Categoria,
    string EventoNombre,
    DateTimeOffset InscripcionDate,
    PaymentMethod PaymentMethod,
    decimal MontoUsd,
    InscriptionStatusAdmin EstadoAdmin,
    string Federacion,
    string LicenciaNumber,
    string? TransaccionId,
    string? Notas);
