using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Inscriptions.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Inscriptions.Commands.CreateInscription;

public sealed record CreateInscriptionCommand(
    Guid CompetitorId,
    Guid EventId,
    Guid CategoryId,
    string? ShirtNumber,
    PaymentMethod PaymentMethod,
    bool Reglamento) : IRequest<InscriptionDto>;
