namespace AlasApp.Application.Abstractions.Persistence;

public interface IAdminSettingsRepository
{
    Task<string?> GetJsonAsync(string key, CancellationToken cancellationToken);

    Task UpsertJsonAsync(string key, string jsonValue, DateTimeOffset timestamp, CancellationToken cancellationToken);
}
