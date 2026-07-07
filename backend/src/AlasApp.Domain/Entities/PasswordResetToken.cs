using AlasApp.Domain.Common;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Domain.Entities;

public sealed class PasswordResetToken : AuditableEntity
{
    private PasswordResetToken()
    {
    }

    private PasswordResetToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAtUtc)
    {
        Id = id;
        UserAccountId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid UserAccountId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? UsedAtUtc { get; private set; }

    public bool IsUsed => UsedAtUtc.HasValue;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;

    public static PasswordResetToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainRuleException("El usuario asociado al token es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainRuleException("El token de recuperación es obligatorio.");
        }

        return new PasswordResetToken(Guid.NewGuid(), userId, tokenHash.Trim(), expiresAtUtc);
    }

    public void MarkUsed(DateTimeOffset timestamp)
    {
        if (IsUsed)
        {
            throw new DomainRuleException("El token de recuperación ya fue utilizado.");
        }

        UsedAtUtc = timestamp;
    }
}
