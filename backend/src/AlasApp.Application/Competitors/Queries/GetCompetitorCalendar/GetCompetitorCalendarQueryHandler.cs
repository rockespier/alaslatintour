using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;

namespace AlasApp.Application.Competitors.Queries.GetCompetitorCalendar;

public sealed class GetCompetitorCalendarQueryHandler(
    ICompetitorRepository competitorRepository,
    IInscriptionRepository inscriptionRepository)
    : IRequestHandler<GetCompetitorCalendarQuery, IReadOnlyCollection<CompetitorCalendarEventDto>>
{
    public async Task<IReadOnlyCollection<CompetitorCalendarEventDto>> Handle(GetCompetitorCalendarQuery request, CancellationToken cancellationToken)
    {
        _ = await competitorRepository.GetByIdAsync(request.CompetitorId, cancellationToken)
            ?? throw new NotFoundException("Competidor no encontrado.");

        return await inscriptionRepository.ListCalendarByCompetitorAsync(request.CompetitorId, cancellationToken);
    }
}
