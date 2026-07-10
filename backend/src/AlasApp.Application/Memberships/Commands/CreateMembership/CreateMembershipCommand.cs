using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Memberships.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Memberships.Commands.CreateMembership;

public sealed record CreateMembershipCommand(
    string ClubFederacion,
    string Pais,
    MembershipPlan Plan,
    DateTimeOffset InicioVigencia,
    DateTimeOffset Vencimiento,
    string EmailContacto) : IRequest<MembershipDto>;
