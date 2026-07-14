using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.EventResults.Models;

namespace AlasApp.Application.EventResults.Commands.UpsertEventResults;

public sealed record UpsertEventResultsCommand(
    Guid EventId,
    Guid CategoryId,
    IReadOnlyCollection<EventResultUpsertItem> Results) : IRequest<IReadOnlyCollection<EventResultDto>>;
