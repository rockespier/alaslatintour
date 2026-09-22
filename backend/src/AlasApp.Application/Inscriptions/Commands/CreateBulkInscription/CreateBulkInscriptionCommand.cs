using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Inscriptions.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Inscriptions.Commands.CreateBulkInscription;

public sealed record CreateBulkInscriptionCommand(
    Guid CompetitorId,
    Guid EventId,
    IReadOnlyCollection<Guid> CategoryIds,
    string? ShirtNumber,
    PaymentMethod PaymentMethod,
    MembershipPlanOption? MembershipPlan,
    bool Reglamento,
    bool RiesgosAceptados,
    bool UsoImagenAceptado) : IRequest<BulkInscriptionDto>;
