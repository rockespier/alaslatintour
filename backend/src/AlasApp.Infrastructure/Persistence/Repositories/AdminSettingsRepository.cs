using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class AdminSettingsRepository(AlasAppDbContext dbContext) : IAdminSettingsRepository
{
    public async Task<string?> GetJsonAsync(string key, CancellationToken cancellationToken)
    {
        var setting = await dbContext.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

        return setting?.JsonValue;
    }

    public async Task UpsertJsonAsync(string key, string jsonValue, DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        var setting = await dbContext.SystemSettings
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

        if (setting is null)
        {
            await dbContext.SystemSettings.AddAsync(SystemSetting.Create(key, jsonValue, timestamp), cancellationToken);
        }
        else
        {
            setting.Update(jsonValue, timestamp);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
