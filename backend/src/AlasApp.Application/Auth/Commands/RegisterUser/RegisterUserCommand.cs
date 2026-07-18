using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Auth.Models;
using AlasApp.Application.IdentityDocuments;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Auth.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string Nombre,
    string Apellido,
    UserType Tipo,
    string Pais,
    PreferredLanguage IdiomaPreferido,
    bool Newsletter,
    bool Terminos,
    bool Reglamento,
    DateTimeOffset? FechaNacimiento,
    CompetitorGender? Genero,
    string Telefono,
    string Club,
    CompetitorPostura? Postura,
    CompetitorShirtSize? TallaCamiseta,
    string Federacion,
    string Patrocinadores,
    IdentityDocumentUpload? IdentityDocument) : IRequest<RegisterResultDto>;
