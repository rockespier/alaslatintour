using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Inscriptions.Models;

namespace AlasApp.Application.Inscriptions.Queries.GetInscriptionById;

public sealed record GetInscriptionByIdQuery(Guid InscriptionId) : IRequest<InscriptionDto>;
