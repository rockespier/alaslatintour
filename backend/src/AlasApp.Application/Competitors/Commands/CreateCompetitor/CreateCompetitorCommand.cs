using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Competitors.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Competitors.Commands.CreateCompetitor;

public sealed record CreateCompetitorCommand(
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
