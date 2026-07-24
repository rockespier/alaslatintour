using AlasApp.Domain.Enums;
using System.Text.Json;

namespace AlasApp.Application.Common;

public static class RolePermissionsSerializer
{
    public const string SettingsKey = "admin_role_permissions";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize(IEnumerable<RolePermissionEntry> entries)
    {
        return JsonSerializer.Serialize(entries.ToList(), JsonOptions);
    }

    public static IReadOnlyCollection<RolePermissionEntry> Deserialize(string json)
    {
        return JsonSerializer.Deserialize<List<RolePermissionEntry>>(json, JsonOptions) ?? [];
    }
}

public sealed record RolePermissionEntry(AdminRole Role, AdminModule Module, PermissionLevel Level);
