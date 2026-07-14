using AlasApp.Api.Authorization;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.AdminSettings;
using AlasApp.Application.AdminSettings.Commands.TestIntegration;
using AlasApp.Application.AdminSettings.Commands.UpdateAdminSettings;
using AlasApp.Application.AdminSettings.Models;
using AlasApp.Application.AdminSettings.Queries.GetAdminSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/admin/settings")]
[Authorize]
public sealed class AdminSettingsController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AdminPolicies.ConfigurationRead)]
    [ProducesResponseType(typeof(AdminSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetAdminSettingsQuery(), cancellationToken);
        return Json(AdminSettingsSerializer.Serialize(result));
    }

    [HttpPut]
    [Authorize(Policy = AdminPolicies.ConfigurationWrite)]
    [ProducesResponseType(typeof(AdminSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        [FromBody] AdminSettingsDto body,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new UpdateAdminSettingsCommand(body), cancellationToken);
        return Json(AdminSettingsSerializer.Serialize(result));
    }

    [HttpPost("integrations/{provider}/test")]
    [Authorize(Policy = AdminPolicies.ConfigurationWrite)]
    [ProducesResponseType(typeof(IntegrationTestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TestIntegration(
        string provider,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new TestIntegrationCommand(provider), cancellationToken);
        return Json(AdminSettingsSerializer.Serialize(result));
    }

    private ContentResult Json(string json)
    {
        return Content(json, "application/json");
    }
}
