using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminUsers.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Security;

namespace AlasApp.Application.AdminUsers.Commands.UpdateRolePermissions;

public sealed class UpdateRolePermissionsCommandHandler(
    IAdminRolePermissionProvider provider,
    IAdminSettingsRepository repository,
    IClock clock)
    : IRequestHandler<UpdateRolePermissionsCommand, RoleDto>
{
    public async Task<RoleDto> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        var snapshot = await provider.GetSnapshotAsync(cancellationToken);
        var updated = new Dictionary<(AdminRole Role, AdminModule Module), PermissionLevel>(snapshot);

        foreach (var (module, level) in request.Permissions)
        {
            updated[(request.Role, module)] = level;
        }

        var entries = updated.Select(x => new RolePermissionEntry(x.Key.Role, x.Key.Module, x.Value)).ToList();
        var json = RolePermissionsSerializer.Serialize(entries);
        await repository.UpsertJsonAsync(RolePermissionsSerializer.SettingsKey, json, clock.UtcNow, cancellationToken);

        provider.SetSnapshot(updated);

        var permissions = AdminRolePermissionMatrix.Modules
            .Select(module => new RolePermissionDto(module, updated[(request.Role, module)]))
            .ToList();

        return new RoleDto(request.Role, permissions);
    }

    private static void Validate(UpdateRolePermissionsCommand request)
    {
        var errors = new List<ValidationError>();

        if (request.Role == AdminRole.SuperAdmin)
        {
            errors.Add(new ValidationError("role", "El rol SuperAdmin no puede modificarse."));
        }

        var expectedModules = AdminRolePermissionMatrix.Modules.ToHashSet();
        var providedModules = request.Permissions.Keys.ToHashSet();

        if (!expectedModules.SetEquals(providedModules))
        {
            errors.Add(new ValidationError("permissions", "Debe enviarse un nivel de permiso para cada modulo del sistema."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Los permisos enviados no son validos.", errors);
        }
    }
}
