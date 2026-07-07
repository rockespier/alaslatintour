using AlasApp.Domain.Enums;

namespace AlasApp.Application.Inscriptions.Models;

public sealed record AdminInscriptionListFilter(
    int Page,
    int Limit,
    Guid? EventId,
    Guid? CategoryId,
    InscriptionStatusAdmin? Status);
