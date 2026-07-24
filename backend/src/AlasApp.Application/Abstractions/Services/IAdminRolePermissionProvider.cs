using AlasApp.Domain.Enums;

namespace AlasApp.Application.Abstractions.Services;

public interface IAdminRolePermissionProvider
{
    Task<PermissionLevel> GetPermissionAsync(AdminRole role, AdminModule module, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<(AdminRole Role, AdminModule Module), PermissionLevel>> GetSnapshotAsync(CancellationToken cancellationToken);

    void SetSnapshot(IReadOnlyDictionary<(AdminRole Role, AdminModule Module), PermissionLevel> snapshot);
}
