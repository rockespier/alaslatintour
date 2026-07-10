using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Common;
using AlasApp.Application.Memberships.Models;

namespace AlasApp.Application.Memberships.Queries.ListMemberships;

public sealed record ListMembershipsQuery(MembershipListFilter Filter) : IRequest<PagedResult<MembershipDto>>;
