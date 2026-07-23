using AlasApp.Api.Authorization;
using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.EventResults.Commands.ImportEventResults;
using AlasApp.Application.EventResults.Commands.UpsertEventResults;
using AlasApp.Application.EventResults.Queries.GetEventResults;
using AlasApp.Application.EventResults.Queries.GetEventResultsRoster;
using AlasApp.Application.EventResults.Queries.GetPrizeDistribution;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Generated = AlasApp.AlasApi.Api.Controllers;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/events/{eventId}")]
public sealed class EventResultsController(IRequestDispatcher dispatcher, IBulkExcelService bulkExcelService) : ControllerBase
{
    private const string SurfScoresAttribution = "Results by SurfScores.com";
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("results")]
    [ProducesResponseType(typeof(Generated.Response), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.Response>> GetResults(
        string eventId,
        [FromQuery] string? categoryId,
        CancellationToken cancellationToken)
    {
        var results = await dispatcher.Send(
            new GetEventResultsQuery(
                ApiContractMapper.ParseGuid(eventId, "eventId"),
                ApiContractMapper.ParseOptionalGuid(categoryId, "categoryId")),
            cancellationToken);

        return Ok(new Generated.Response(
            SurfScoresAttribution,
            results.Select(ApiContractMapper.ToContract).ToList()));
    }

    [HttpPost("results")]
    [Authorize(Policy = AdminPolicies.EventsWrite)]
    [ProducesResponseType(typeof(Generated.Response2), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.Response2>> UpsertResults(
        string eventId,
        [FromBody] Generated.Body2 body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return BadRequest(new { message = "Debes enviar los resultados del evento." });
        }

        var results = await dispatcher.Send(
            new UpsertEventResultsCommand(
                ApiContractMapper.ParseGuid(eventId, "eventId"),
                ApiContractMapper.ParseGuid(body.CategoryId, "categoryId"),
                (body.Results ?? []).Select(ApiContractMapper.ToEventResultUpsertItem).ToList()),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new Generated.Response2(results.Select(ApiContractMapper.ToContract).ToList()));
    }

    [HttpGet("results/template")]
    [Authorize(Policy = AdminPolicies.EventsWrite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadResultsTemplate(
        string eventId,
        [FromQuery] string categoryId,
        CancellationToken cancellationToken)
    {
        var roster = await dispatcher.Send(
            new GetEventResultsRosterQuery(
                ApiContractMapper.ParseGuid(eventId, "eventId"),
                ApiContractMapper.ParseGuid(categoryId, "categoryId")),
            cancellationToken);

        return File(bulkExcelService.BuildEventResultsTemplate(roster), ExcelContentType, "resultados-template.xlsx");
    }

    [HttpPost("results/import")]
    [Authorize(Policy = AdminPolicies.EventsWrite)]
    [ProducesResponseType(typeof(BulkImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BulkImportResponse>> ImportResults(
        string eventId,
        [FromQuery] string categoryId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest("El archivo XLSX no puede estar vacío.");
        }

        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);

        var result = await dispatcher.Send(
            new ImportEventResultsCommand(
                ApiContractMapper.ParseGuid(eventId, "eventId"),
                ApiContractMapper.ParseGuid(categoryId, "categoryId"),
                memory.ToArray()),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("prize-distribution")]
    [ProducesResponseType(typeof(Generated.Response3), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.Response3>> GetPrizeDistribution(
        string eventId,
        CancellationToken cancellationToken)
    {
        var distribution = await dispatcher.Send(
            new GetPrizeDistributionQuery(ApiContractMapper.ParseGuid(eventId, "eventId")),
            cancellationToken);

        return Ok(new Generated.Response3(
            distribution.Rows.Select(ApiContractMapper.ToContract).ToList(),
            distribution.Stars));
    }
}
