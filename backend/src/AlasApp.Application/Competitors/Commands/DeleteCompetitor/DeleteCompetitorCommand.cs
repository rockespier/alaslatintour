using AlasApp.Application.Abstractions.Messaging;

namespace AlasApp.Application.Competitors.Commands.DeleteCompetitor;

public sealed record DeleteCompetitorCommand(Guid CompetitorId) : IRequest<bool>;
