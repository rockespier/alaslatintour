using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Events.Commands.DeleteEvent;
using AlasApp.Application.Events.Models;
using AlasApp.Application.Events.Queries.GetEventById;
using AlasApp.Application.Events.Queries.ListEvents;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/events")]
public sealed class EventsController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Generated.EventListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.EventListResponse>> List(
        [FromQuery] int? page,
        [FromQuery] int? limit,
        [FromQuery] string? circuitId,
        [FromQuery] string? status,
        [FromQuery] string? country,
        [FromQuery] int? year,
        [FromQuery] int? stars,
        CancellationToken cancellationToken)
    {
        Guid? parsedCircuitId = string.IsNullOrWhiteSpace(circuitId)
            ? null
            : ApiContractMapper.ParseGuid(circuitId, "circuitId");

        var query = new ListEventsQuery(
            new EventListFilter(
                page ?? 1,
                limit ?? 20,
                parsedCircuitId,
                ApiContractMapper.ParseEventStatusPublic(status),
                country,
                year,
                stars));

        var result = await dispatcher.Send(query, cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("{eventId}")]
    [ProducesResponseType(typeof(Generated.EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.EventResponse>> GetById(string eventId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetEventByIdQuery(ApiContractMapper.ParseGuid(eventId, "eventId")),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Generated.EventResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<Generated.EventResponse>> Create([FromBody] Generated.EventRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(body), cancellationToken);
        var contract = ApiContractMapper.ToContract(result);

        return CreatedAtAction(nameof(GetById), new { eventId = contract.Id }, contract);
    }

    [HttpPut("{eventId}")]
    [ProducesResponseType(typeof(Generated.EventResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.EventResponse>> Update(string eventId, [FromBody] Generated.EventRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            ApiContractMapper.ToCommand(ApiContractMapper.ParseGuid(eventId, "eventId"), body),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpDelete("{eventId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string eventId, CancellationToken cancellationToken)
    {
        await dispatcher.Send(
            new DeleteEventCommand(ApiContractMapper.ParseGuid(eventId, "eventId")),
            cancellationToken);

        return NoContent();
    }
}
