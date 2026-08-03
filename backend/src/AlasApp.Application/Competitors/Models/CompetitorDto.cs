using AlasApp.Domain.Enums;

namespace AlasApp.Application.Competitors.Models;

public sealed record CompetitorDto(
    Guid Id,
    string Nombre,
    string Apellido,
    string Email,
    DateTimeOffset FechaNacimiento,
    CompetitorGender Genero,
    string Pais,
    string Telefono,
    string Club,
    CompetitorPostura Postura,
    CompetitorShirtSize TallaCamiseta,
    string NumeroCamiseta,
    string Patrocinadores,
    string Federacion,
    string SurfScoresCode,
    CompetitorLicenseDto License,
    bool HasIdentityDocument,
    bool NotificationEmail,
    bool NotificationPush,
    bool NotificationResultados,
    bool NotificationInscripciones,
    DateTimeOffset CreatedAtUtc);
