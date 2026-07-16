using AlasApp.Api.Authorization;
using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.SurfScoresImport.Commands.ImportSurfScoresEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/circuits/{circuitId}/surfscores-import")]
public sealed class SurfScoresImportController(IRequestDispatcher dispatcher) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpPost]
    [Authorize(Policy = AdminPolicies.CircuitsWrite)]
    public async Task<IActionResult> Import(string circuitId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new ImportSurfScoresEventsCommand(ApiContractMapper.ParseGuid(circuitId, "circuitId")),
            cancellationToken);

        return Content(JsonSerializer.Serialize(result, JsonOptions), "application/json");
    }
}
