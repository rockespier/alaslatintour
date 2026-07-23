using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.EventResults.Models;

namespace AlasApp.Application.EventResults.Commands.UpsertEventResults;

public sealed class UpsertEventResultsCommandHandler(EventResultsWriter writer)
    : IRequestHandler<UpsertEventResultsCommand, IReadOnlyCollection<EventResultDto>>
{
    public async Task<IReadOnlyCollection<EventResultDto>> Handle(UpsertEventResultsCommand request, CancellationToken cancellationToken)
    {
        var outcome = await writer.UpsertAsync(request.EventId, request.CategoryId, request.Results, cancellationToken);
        return outcome.Results;
    }
}
