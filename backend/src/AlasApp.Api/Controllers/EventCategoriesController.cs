using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.EventCategories.Queries.GetEventCategories;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/events/{eventId}/categories")]
public sealed class EventCategoriesController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Generated.Response), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.Response>> List(string eventId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetEventCategoriesQuery(ApiContractMapper.ParseGuid(eventId, "eventId")),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPut]
    [ProducesResponseType(typeof(Generated.Response2), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.Response2>> Update(string eventId, [FromBody] Generated.Body body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            ApiContractMapper.ToCommand(ApiContractMapper.ParseGuid(eventId, "eventId"), body),
            cancellationToken);

        return Ok(ApiContractMapper.ToUpdatedEventCategoriesContract(result));
    }
}
