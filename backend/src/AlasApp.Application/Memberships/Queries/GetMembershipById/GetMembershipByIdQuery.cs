using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Memberships.Models;

namespace AlasApp.Application.Memberships.Queries.GetMembershipById;

public sealed record GetMembershipByIdQuery(Guid MembershipId) : IRequest<MembershipDto>;
