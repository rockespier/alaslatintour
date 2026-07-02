using AlasApp.Application.Abstractions.Services;

namespace AlasApp.Application.Common;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
