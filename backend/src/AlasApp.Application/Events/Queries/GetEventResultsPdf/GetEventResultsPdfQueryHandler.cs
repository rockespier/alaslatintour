using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;

namespace AlasApp.Application.Events.Queries.GetEventResultsPdf;

public sealed class GetEventResultsPdfQueryHandler(
    IEventRepository eventRepository,
    IWordPressService wordPressService)
    : IRequestHandler<GetEventResultsPdfQuery, string?>
{
    public async Task<string?> Handle(GetEventResultsPdfQuery request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException("Evento no encontrado.");

        if (string.IsNullOrWhiteSpace(@event.SurfScoresCode))
        {
            return null;
        }

        return await wordPressService.FindResultsPdfUrlAsync(@event.SurfScoresCode, cancellationToken);
    }
}
