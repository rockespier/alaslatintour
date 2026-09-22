namespace AlasApp.Application.Inscriptions.Models;

public sealed record BulkInscriptionDto(
    Guid InscriptionGroupId,
    Guid PrimaryInscriptionId,
    IReadOnlyCollection<Guid> InscriptionIds,
    decimal TotalMontoUsd);
