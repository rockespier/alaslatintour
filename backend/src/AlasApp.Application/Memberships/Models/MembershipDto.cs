using AlasApp.Domain.Enums;

namespace AlasApp.Application.Memberships.Models;

public sealed record MembershipDto(
    Guid Id,
    string ClubFederacion,
    string Pais,
    MembershipPlan Plan,
    DateTimeOffset InicioVigencia,
    DateTimeOffset Vencimiento,
    string EmailContacto,
    int CompetidoresAfiliados,
    MembershipStatus Estado,
    DateTimeOffset CreatedAt);
