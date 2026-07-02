namespace AlasApp.Domain.Common;

public abstract class AuditableEntity
{
    public Guid Id { get; protected set; }

    public DateTimeOffset CreatedAtUtc { get; protected set; }

    public DateTimeOffset UpdatedAtUtc { get; protected set; }

    public void SetCreated(DateTimeOffset timestamp)
    {
        CreatedAtUtc = timestamp;
        UpdatedAtUtc = timestamp;
    }

    public void SetUpdated(DateTimeOffset timestamp)
    {
        UpdatedAtUtc = timestamp;
    }
}
