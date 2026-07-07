using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Inscriptions.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Inscriptions.Commands.UpdateInscription;

public sealed record UpdateInscriptionCommand(
    Guid InscriptionId,
    string? ShirtNumber,
    InscriptionStatusAdmin? EstadoAdmin,
    string? Notes) : IRequest<InscriptionDto>;
