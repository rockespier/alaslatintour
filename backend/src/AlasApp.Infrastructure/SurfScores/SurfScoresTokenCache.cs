namespace AlasApp.Infrastructure.SurfScores;

public sealed class SurfScoresTokenCache
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiry = DateTimeOffset.MinValue;

    public async Task<string> GetOrRefreshAsync(
        Func<CancellationToken, Task<(string Token, int ExpiresIn)>> fetchToken,
        CancellationToken cancellationToken)
    {
        if (_token is not null && DateTimeOffset.UtcNow < _expiry)
        {
            return _token;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_token is not null && DateTimeOffset.UtcNow < _expiry)
            {
                return _token;
            }

            var (token, expiresIn) = await fetchToken(cancellationToken);
            _token = token;
            _expiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);
            return _token;
        }
        finally
        {
            _lock.Release();
        }
    }
}
