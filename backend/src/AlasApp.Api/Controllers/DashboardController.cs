using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Dashboard.Queries.GetDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Generated = AlasApp.AlasApi.Api.Controllers;
using AlasApp.Api.Authorization;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/admin/dashboard")]
[Authorize(Policy = AdminPolicies.DashboardRead)]
public sealed class DashboardController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Generated.DashboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.DashboardResponse>> Get(CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetDashboardQuery(), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }
}
