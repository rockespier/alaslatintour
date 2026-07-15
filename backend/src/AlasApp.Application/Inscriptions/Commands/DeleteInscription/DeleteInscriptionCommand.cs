using AlasApp.Application.Abstractions.Messaging;

namespace AlasApp.Application.Inscriptions.Commands.DeleteInscription;

public sealed record DeleteInscriptionCommand(Guid InscriptionId, Guid CompetitorId) : IRequest<bool>;
