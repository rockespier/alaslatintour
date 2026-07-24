using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Security;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace AlasApp.Application.Common;

/// <summary>
/// In-memory cache of role permissions, lazily hydrated once from persisted settings.
/// Registered as a singleton so the (also singleton) authorization handler can read
/// permissions synchronously-fast without hitting the database on every request.
/// </summary>
public sealed class AdminRolePermissionProvider(IServiceScopeFactory scopeFactory) : IAdminRolePermissionProvider
{
    private static readonly AdminRole[] AllRoles =
    [
        AdminRole.SuperAdmin,
        AdminRole.Admin,
        AdminRole.Arbitro,
        AdminRole.Revisor
    ];

    private readonly ConcurrentDictionary<(AdminRole, AdminModule), PermissionLevel> _cache = new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private volatile bool _loaded;

    public async Task<PermissionLevel> GetPermissionAsync(AdminRole role, AdminModule module, CancellationToken cancellationToken)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _cache.GetValueOrDefault((role, module), AdminRolePermissionMatrix.GetPermission(role, module));
    }

    public async Task<IReadOnlyDictionary<(AdminRole Role, AdminModule Module), PermissionLevel>> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        await EnsureLoadedAsync(cancellationToken);
        return BuildSnapshot();
    }

    public void SetSnapshot(IReadOnlyDictionary<(AdminRole Role, AdminModule Module), PermissionLevel> snapshot)
    {
        foreach (var entry in snapshot)
        {
            _cache[entry.Key] = entry.Value;
        }

        _loaded = true;
    }

    private IReadOnlyDictionary<(AdminRole, AdminModule), PermissionLevel> BuildSnapshot()
    {
        var result = new Dictionary<(AdminRole, AdminModule), PermissionLevel>();

        foreach (var role in AllRoles)
        {
            foreach (var module in AdminRolePermissionMatrix.Modules)
            {
                result[(role, module)] = _cache.GetValueOrDefault((role, module), AdminRolePermissionMatrix.GetPermission(role, module));
            }
        }

        return result;
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
        {
            return;
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_loaded)
            {
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAdminSettingsRepository>();
            var json = await repository.GetJsonAsync(RolePermissionsSerializer.SettingsKey, cancellationToken);

            if (!string.IsNullOrWhiteSpace(json))
            {
                foreach (var entry in RolePermissionsSerializer.Deserialize(json))
                {
                    _cache[(entry.Role, entry.Module)] = entry.Level;
                }
            }

            _loaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }
}
