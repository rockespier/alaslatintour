using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Inscriptions.Models;

namespace AlasApp.Application.Inscriptions.Queries.ListConfirmedInscriptions;

public sealed record ListConfirmedInscriptionsQuery(Guid EventId) : IRequest<IReadOnlyCollection<ConfirmedInscriptionRowDto>>;
