using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Inscriptions.Models;

namespace AlasApp.Application.Inscriptions.Queries.ListInscriptions;

public sealed class ListInscriptionsQueryHandler(IInscriptionRepository inscriptionRepository)
    : IRequestHandler<ListInscriptionsQuery, PagedResult<AdminInscriptionRowDto>>
{
    public Task<PagedResult<AdminInscriptionRowDto>> Handle(ListInscriptionsQuery request, CancellationToken cancellationToken)
    {
        return inscriptionRepository.ListAdminAsync(request.Filter, cancellationToken);
    }
}
