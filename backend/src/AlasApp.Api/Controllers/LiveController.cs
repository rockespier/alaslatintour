using System.Text.Json;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Live.Queries.GetPublicLiveStatus;
using AlasApp.Application.Live.Models;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/live")]
public sealed class LiveController(IRequestDispatcher dispatcher) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet]
    [ProducesResponseType(typeof(PublicLiveStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetPublicLiveStatusQuery(), cancellationToken);
        return Content(JsonSerializer.Serialize(result, JsonOptions), "application/json");
    }
}
