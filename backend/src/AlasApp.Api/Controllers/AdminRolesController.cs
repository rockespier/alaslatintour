using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.AdminUsers.Commands.UpdateRolePermissions;
using AlasApp.Application.AdminUsers.Queries.ListAdminRoles;
using AlasApp.Application.Common;
using AlasApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Generated = AlasApp.AlasApi.Api.Controllers;
using AlasApp.Api.Authorization;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/admin/roles")]
[Authorize]
public sealed class AdminRolesController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AdminPolicies.ConfigurationRead)]
    [ProducesResponseType(typeof(Generated.RoleListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.RoleListResponse>> List(CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new ListAdminRolesQuery(), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPut("{role}/permissions")]
    [Authorize(Policy = AdminPolicies.ConfigurationWrite)]
    [ProducesResponseType(typeof(Generated.RoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Generated.RoleResponse>> UpdatePermissions(
        string role,
        [FromBody] UpdateRolePermissionsRequest body,
        CancellationToken cancellationToken)
    {
        if (!TryParseRole(role, out var parsedRole))
        {
            throw new ValidationException(
                "El rol especificado no es valido.",
                [new ValidationError("role", "El rol especificado no es valido.")]);
        }

        var permissions = body.Permissions
            .GroupBy(x => ApiContractMapper.ToDomainAdminModule(x.Module))
            .ToDictionary(g => g.Key, g => ApiContractMapper.ToDomainPermissionLevel(g.Last().Level));

        var result = await dispatcher.Send(new UpdateRolePermissionsCommand(parsedRole, permissions), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }

    // Accepts both the domain enum member name (e.g. "SuperAdmin") and the display
    // name used by the generated contracts (e.g. "Super Admin", "Árbitro").
    private static bool TryParseRole(string value, out AdminRole role)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "superadmin":
            case "super admin":
            case "super_admin":
                role = AdminRole.SuperAdmin;
                return true;
            case "admin":
                role = AdminRole.Admin;
                return true;
            case "arbitro":
            case "árbitro":
                role = AdminRole.Arbitro;
                return true;
            case "revisor":
                role = AdminRole.Revisor;
                return true;
            default:
                role = default;
                return false;
        }
    }
}
