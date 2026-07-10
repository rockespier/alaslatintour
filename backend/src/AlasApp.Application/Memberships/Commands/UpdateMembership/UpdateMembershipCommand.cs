using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Memberships.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Memberships.Commands.UpdateMembership;

public sealed record UpdateMembershipCommand(
    Guid MembershipId,
    string ClubFederacion,
    string Pais,
    MembershipPlan Plan,
    DateTimeOffset InicioVigencia,
    DateTimeOffset Vencimiento,
    string EmailContacto) : IRequest<MembershipDto>;
