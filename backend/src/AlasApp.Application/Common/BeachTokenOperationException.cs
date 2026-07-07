namespace AlasApp.Application.Common;

public sealed class BeachTokenOperationException(
    string errorCode,
    string message,
    DateTimeOffset? issuedAt = null,
    DateTimeOffset? expiredAt = null) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;

    public DateTimeOffset? IssuedAt { get; } = issuedAt;

    public DateTimeOffset? ExpiredAt { get; } = expiredAt;
}
