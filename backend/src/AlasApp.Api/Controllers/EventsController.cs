using AlasApp.Api.Authorization;
using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Events.Commands.DeleteEvent;
using AlasApp.Application.Events.Commands.ImportEvents;
using AlasApp.Application.Events.Models;
using AlasApp.Application.Events.Queries.GetEventById;
using AlasApp.Application.Events.Queries.ListEvents;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/events")]
public sealed class EventsController(IRequestDispatcher dispatcher, IBulkExcelService bulkExcelService) : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

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
    [Authorize(Policy = AdminPolicies.EventsWrite)]
    [ProducesResponseType(typeof(Generated.EventResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<Generated.EventResponse>> Create([FromBody] Generated.EventRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(body), cancellationToken);
        var contract = ApiContractMapper.ToContract(result);

        return CreatedAtAction(nameof(GetById), new { eventId = contract.Id }, contract);
    }

    [HttpPut("{eventId}")]
    [Authorize(Policy = AdminPolicies.EventsWrite)]
    [ProducesResponseType(typeof(Generated.EventResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.EventResponse>> Update(string eventId, [FromBody] Generated.EventRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            ApiContractMapper.ToCommand(ApiContractMapper.ParseGuid(eventId, "eventId"), body),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpDelete("{eventId}")]
    [Authorize(Policy = AdminPolicies.EventsWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string eventId, CancellationToken cancellationToken)
    {
        await dispatcher.Send(
            new DeleteEventCommand(ApiContractMapper.ParseGuid(eventId, "eventId")),
            cancellationToken);

        return NoContent();
    }

    [HttpGet("template")]
    [Authorize(Policy = AdminPolicies.EventsWrite)]
    public IActionResult DownloadTemplate()
    {
        return File(bulkExcelService.BuildEventsTemplate(), ExcelContentType, "events-template.xlsx");
    }

    [HttpPost("import")]
    [Authorize(Policy = AdminPolicies.EventsWrite)]
    [ProducesResponseType(typeof(BulkImportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkImportResponse>> Import([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest("El archivo XLSX no puede estar vacío.");
        }

        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);

        var result = await dispatcher.Send(new ImportEventsCommand(memory.ToArray()), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }
}
