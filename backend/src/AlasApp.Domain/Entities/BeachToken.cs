using AlasApp.Domain.Common;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Domain.Entities;

public sealed class BeachToken : AuditableEntity
{
    private BeachToken()
    {
    }

    private BeachToken(Guid id, Guid inscriptionId, DateTimeOffset requestedAt)
    {
        Id = id;
        InscriptionId = inscriptionId;
        RequestedAt = requestedAt;
        Status = TokenHistoryStatus.Pendiente;
    }

    public Guid InscriptionId { get; private set; }

    public Inscription? Inscription { get; private set; }

    public string? TokenCode { get; private set; }

    public TokenHistoryStatus Status { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    public DateTimeOffset? GeneratedAt { get; private set; }

    public DateTimeOffset? ExpirationAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public static BeachToken Create(Guid inscriptionId, DateTimeOffset requestedAt)
    {
        if (inscriptionId == Guid.Empty)
        {
            throw new DomainRuleException("La inscripcion del token es obligatoria.");
        }

        return new BeachToken(Guid.NewGuid(), inscriptionId, requestedAt);
    }

    public void Approve(string tokenCode, DateTimeOffset generatedAt, DateTimeOffset expirationAt)
    {
        EnsurePending();

        if (string.IsNullOrWhiteSpace(tokenCode) || tokenCode.Trim().Length > 20)
        {
            throw new DomainRuleException("El codigo del token es obligatorio y no puede exceder 20 caracteres.");
        }

        if (expirationAt <= generatedAt)
        {
            throw new DomainRuleException("La expiracion del token debe ser posterior a su generacion.");
        }

        TokenCode = tokenCode.Trim().ToUpperInvariant();
        GeneratedAt = generatedAt;
        ExpirationAt = expirationAt;
        RejectionReason = null;
    }

    public void Reject(string reason)
    {
        EnsurePending();

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
        {
            throw new DomainRuleException("El motivo del rechazo debe tener al menos 10 caracteres.");
        }

        Status = TokenHistoryStatus.Rechazado;
        RejectionReason = reason.Trim();
    }

    public void MarkUsed(DateTimeOffset usedAt)
    {
        EnsurePending();

        if (TokenCode is null || GeneratedAt is null || ExpirationAt is null)
        {
            throw new DomainRuleException("El token aun no fue aprobado.");
        }

        Status = TokenHistoryStatus.Usado;
        UsedAt = usedAt;
    }

    public void MarkExpired()
    {
        if (Status == TokenHistoryStatus.Pendiente && ExpirationAt.HasValue)
        {
            Status = TokenHistoryStatus.Expirado;
        }
    }

    public bool IsApproved() => TokenCode is not null && GeneratedAt.HasValue && ExpirationAt.HasValue;

    private void EnsurePending()
    {
        if (Status != TokenHistoryStatus.Pendiente)
        {
            throw new DomainRuleException("La solicitud de token ya fue procesada.");
        }
    }
}
