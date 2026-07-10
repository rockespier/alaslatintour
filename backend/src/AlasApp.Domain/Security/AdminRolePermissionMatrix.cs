using AlasApp.Domain.Enums;

namespace AlasApp.Domain.Security;

public static class AdminRolePermissionMatrix
{
    private static readonly AdminModule[] AllModules =
    [
        AdminModule.Dashboard,
        AdminModule.Usuarios,
        AdminModule.Circuitos,
        AdminModule.Eventos,
        AdminModule.Categorias,
        AdminModule.Inscritos,
        AdminModule.Pagos,
        AdminModule.Tokens,
        AdminModule.Configuracion
    ];

    public static IReadOnlyCollection<AdminModule> Modules => AllModules;

    public static IReadOnlyCollection<KeyValuePair<AdminModule, PermissionLevel>> GetPermissions(AdminRole role)
    {
        return AllModules
            .Select(module => new KeyValuePair<AdminModule, PermissionLevel>(module, GetPermission(role, module)))
            .ToList();
    }

    public static PermissionLevel GetPermission(AdminRole role, AdminModule module)
    {
        return role switch
        {
            AdminRole.SuperAdmin => PermissionLevel.Full,
            AdminRole.Admin => module == AdminModule.Configuracion ? PermissionLevel.ReadOnly : PermissionLevel.Full,
            AdminRole.Arbitro => module switch
            {
                AdminModule.Dashboard => PermissionLevel.ReadOnly,
                AdminModule.Eventos => PermissionLevel.Full,
                AdminModule.Inscritos => PermissionLevel.Full,
                AdminModule.Categorias => PermissionLevel.ReadOnly,
                _ => PermissionLevel.None
            },
            AdminRole.Revisor => module switch
            {
                AdminModule.Dashboard => PermissionLevel.ReadOnly,
                AdminModule.Usuarios => PermissionLevel.ReadOnly,
                AdminModule.Circuitos => PermissionLevel.ReadOnly,
                AdminModule.Eventos => PermissionLevel.ReadOnly,
                AdminModule.Categorias => PermissionLevel.ReadOnly,
                AdminModule.Inscritos => PermissionLevel.ReadOnly,
                AdminModule.Pagos => PermissionLevel.ReadOnly,
                AdminModule.Tokens => PermissionLevel.ReadOnly,
                _ => PermissionLevel.None
            },
            _ => PermissionLevel.None
        };
    }

    public static bool HasPermission(AdminRole role, AdminModule module, PermissionLevel requiredLevel)
    {
        var actualLevel = GetPermission(role, module);

        return requiredLevel switch
        {
            PermissionLevel.ReadOnly => actualLevel is PermissionLevel.ReadOnly or PermissionLevel.Full,
            PermissionLevel.Full => actualLevel == PermissionLevel.Full,
            PermissionLevel.None => true,
            _ => false
        };
    }
}
