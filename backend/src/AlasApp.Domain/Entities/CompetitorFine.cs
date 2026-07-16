using AlasApp.Domain.Common;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Domain.Entities;

public sealed class CompetitorFine : AuditableEntity
{
    private CompetitorFine()
    {
    }

    private CompetitorFine(
        Guid id,
        Guid competitorId,
        decimal amountUsd,
        string reason,
        string notes,
        Guid createdByUserId)
    {
        Id = id;
        CompetitorId = competitorId;
        AmountUsd = amountUsd;
        Reason = reason;
        Notes = notes;
        CreatedByUserId = createdByUserId;
        Status = CompetitorFineStatus.Pendiente;
    }

    public Guid CompetitorId { get; private set; }

    public decimal AmountUsd { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public string Notes { get; private set; } = string.Empty;

    public CompetitorFineStatus Status { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public static CompetitorFine Create(
        Guid competitorId,
        decimal amountUsd,
        string reason,
        string? notes,
        Guid createdByUserId)
    {
        Validate(competitorId, amountUsd, reason, createdByUserId);

        return new CompetitorFine(
            Guid.NewGuid(),
            competitorId,
            decimal.Round(amountUsd, 2, MidpointRounding.AwayFromZero),
            reason.Trim(),
            NormalizeOptional(notes),
            createdByUserId);
    }

    public void Update(decimal amountUsd, string reason, string? notes, CompetitorFineStatus status)
    {
        Validate(CompetitorId, amountUsd, reason, CreatedByUserId);

        AmountUsd = decimal.Round(amountUsd, 2, MidpointRounding.AwayFromZero);
        Reason = reason.Trim();
        Notes = NormalizeOptional(notes);
        Status = status;
    }

    private static void Validate(Guid competitorId, decimal amountUsd, string reason, Guid createdByUserId)
    {
        if (competitorId == Guid.Empty)
        {
            throw new DomainRuleException("El competidor de la multa es obligatorio.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainRuleException("El usuario que registra la multa es obligatorio.");
        }

        if (amountUsd <= 0)
        {
            throw new DomainRuleException("El importe de la multa debe ser mayor a cero.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException("El motivo de la multa es obligatorio.");
        }
    }

    private static string NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
