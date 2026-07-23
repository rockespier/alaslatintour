using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.EventResults.Models;
using AlasApp.Application.Inscriptions.Models;

namespace AlasApp.Application.EventResults.Queries.GetEventResultsRoster;

public sealed class GetEventResultsRosterQueryHandler(
    IEventRepository eventRepository,
    IInscriptionRepository inscriptionRepository,
    IEventResultRepository eventResultRepository)
    : IRequestHandler<GetEventResultsRosterQuery, IReadOnlyCollection<EventResultRosterRowDto>>
{
    private const int MaxRosterRows = 1000;

    public async Task<IReadOnlyCollection<EventResultRosterRowDto>> Handle(GetEventResultsRosterQuery request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetEntityByIdAsync(request.EventId, cancellationToken);
        if (@event is null)
        {
            throw new NotFoundException("Evento no encontrado.");
        }

        if (@event.Categories.All(x => x.CategoryId != request.CategoryId))
        {
            throw new ValidationException(
                "La categoria no esta habilitada para el evento.",
                [new ValidationError("categoryId", "La categoria no esta habilitada para el evento.")]);
        }

        var inscriptions = await inscriptionRepository.ListAdminAsync(
            new AdminInscriptionListFilter(1, MaxRosterRows, request.EventId, request.CategoryId, null),
            cancellationToken);

        var results = await eventResultRepository.ListAsync(request.EventId, request.CategoryId, cancellationToken);
        var resultsByCompetitor = results.ToDictionary(x => x.CompetitorId);

        return inscriptions.Items
            .Select(inscription =>
            {
                resultsByCompetitor.TryGetValue(inscription.CompetitorId, out var result);
                return new EventResultRosterRowDto(
                    inscription.CompetitorId,
                    inscription.FullName,
                    inscription.Country,
                    result?.Place,
                    result?.LigaPoints,
                    result?.PrizeUsd,
                    result?.HeatOla1,
                    result?.HeatOla2);
            })
            .OrderBy(x => x.CompetitorName)
            .ToList();
    }
}
