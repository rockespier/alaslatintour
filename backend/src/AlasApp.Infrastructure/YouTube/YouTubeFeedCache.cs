using AlasApp.Application.Videos.Models;

namespace AlasApp.Infrastructure.YouTube;

/// <summary>Singleton holder for the last feed read, so YouTube is hit at most once per TTL.</summary>
public sealed class YouTubeFeedCache
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IReadOnlyCollection<VideoDto>? _videos;
    private DateTimeOffset _expiry = DateTimeOffset.MinValue;

    public async Task<IReadOnlyCollection<VideoDto>> GetOrRefreshAsync(
        Func<CancellationToken, Task<IReadOnlyCollection<VideoDto>>> fetch,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        if (_videos is not null && DateTimeOffset.UtcNow < _expiry)
        {
            return _videos;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_videos is not null && DateTimeOffset.UtcNow < _expiry)
            {
                return _videos;
            }

            try
            {
                _videos = await fetch(cancellationToken);
                _expiry = DateTimeOffset.UtcNow.Add(ttl);
            }
            catch (Exception) when (_videos is not null)
            {
                // YouTube unreachable: keep serving the last good list and retry in a minute.
                _expiry = DateTimeOffset.UtcNow.AddMinutes(1);
            }

            return _videos;
        }
        finally
        {
            _lock.Release();
        }
    }
}
