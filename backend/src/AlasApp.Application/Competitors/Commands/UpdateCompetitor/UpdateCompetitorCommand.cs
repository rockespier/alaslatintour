using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Competitors.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Competitors.Commands.UpdateCompetitor;

public sealed record UpdateCompetitorCommand(
    Guid CompetitorId,
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
    string? SurfScoresCode = null) : IRequest<CompetitorDto>;
