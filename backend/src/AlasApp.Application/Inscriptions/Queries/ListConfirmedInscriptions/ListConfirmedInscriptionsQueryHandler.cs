using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Inscriptions.Models;

namespace AlasApp.Application.Inscriptions.Queries.ListConfirmedInscriptions;

public sealed class ListConfirmedInscriptionsQueryHandler(
    IEventRepository eventRepository,
    IInscriptionRepository inscriptionRepository)
    : IRequestHandler<ListConfirmedInscriptionsQuery, IReadOnlyCollection<ConfirmedInscriptionRowDto>>
{
    public async Task<IReadOnlyCollection<ConfirmedInscriptionRowDto>> Handle(ListConfirmedInscriptionsQuery request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null)
        {
            throw new NotFoundException("Evento no encontrado.");
        }

        return await inscriptionRepository.ListConfirmedPublicAsync(request.EventId, cancellationToken);
    }
}
