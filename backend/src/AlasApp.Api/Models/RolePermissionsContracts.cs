using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Generated = AlasApp.AlasApi.Api.Controllers;

namespace AlasApp.Api.Models;

// Mirrors the wire format of Generated.RolePermission (module/level display names,
// e.g. "Categorías", "read-only") so the admin UI can round-trip values it already
// received from GET /admin/roles without a separate translation table.
public sealed class RolePermissionEntryRequest
{
    [JsonConverter(typeof(StringEnumConverter))]
    public Generated.AdminModule Module { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public Generated.PermissionLevel Level { get; set; }
}

public sealed class UpdateRolePermissionsRequest
{
    public List<RolePermissionEntryRequest> Permissions { get; set; } = [];
}
