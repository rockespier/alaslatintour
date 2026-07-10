using AlasApp.Application.Abstractions.Messaging;

namespace AlasApp.Application.Memberships.Commands.DeleteMembership;

public sealed record DeleteMembershipCommand(Guid MembershipId) : IRequest<bool>;
