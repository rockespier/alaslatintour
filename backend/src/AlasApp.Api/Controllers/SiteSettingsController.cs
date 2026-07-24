using System.Text.Json;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.PublicSiteSettings.Models;
using AlasApp.Application.PublicSiteSettings.Queries.GetPublicSiteSettings;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/site-settings")]
public sealed class SiteSettingsController(IRequestDispatcher dispatcher) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet]
    [ProducesResponseType(typeof(PublicSiteSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetPublicSiteSettingsQuery(), cancellationToken);
        return Content(JsonSerializer.Serialize(result, JsonOptions), "application/json");
    }
}
