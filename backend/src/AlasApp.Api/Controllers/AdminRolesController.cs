using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.AdminUsers.Queries.ListAdminRoles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Generated = AlasApp.AlasApi.Api.Controllers;
using AlasApp.Api.Authorization;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/admin/roles")]
[Authorize(Policy = AdminPolicies.ConfigurationRead)]
public sealed class AdminRolesController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Generated.RoleListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.RoleListResponse>> List(CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new ListAdminRolesQuery(), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }
}
