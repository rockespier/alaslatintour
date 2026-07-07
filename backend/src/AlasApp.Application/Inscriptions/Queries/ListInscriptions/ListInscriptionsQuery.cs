using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Common;
using AlasApp.Application.Inscriptions.Models;

namespace AlasApp.Application.Inscriptions.Queries.ListInscriptions;

public sealed record ListInscriptionsQuery(AdminInscriptionListFilter Filter) : IRequest<PagedResult<AdminInscriptionRowDto>>;
