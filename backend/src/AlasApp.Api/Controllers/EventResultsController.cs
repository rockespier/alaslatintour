using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.EventResults.Commands.UpsertEventResults;
using AlasApp.Application.EventResults.Queries.GetEventResults;
using AlasApp.Application.EventResults.Queries.GetPrizeDistribution;
using Microsoft.AspNetCore.Mvc;
using Generated = AlasApp.AlasApi.Api.Controllers;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/events/{eventId}")]
public sealed class EventResultsController(IRequestDispatcher dispatcher) : ControllerBase
{
    private const string SurfScoresAttribution = "Results by SurfScores.com";

    [HttpGet("results")]
    [ProducesResponseType(typeof(Generated.Response3), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.Response3>> GetResults(
        string eventId,
        [FromQuery] string? categoryId,
        CancellationToken cancellationToken)
    {
        var results = await dispatcher.Send(
            new GetEventResultsQuery(
                ApiContractMapper.ParseGuid(eventId, "eventId"),
                ApiContractMapper.ParseOptionalGuid(categoryId, "categoryId")),
            cancellationToken);

        return Ok(new Generated.Response3(
            SurfScoresAttribution,
            results.Select(ApiContractMapper.ToContract).ToList()));
    }

    [HttpPost("results")]
    [ProducesResponseType(typeof(Generated.Response4), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.Response4>> UpsertResults(
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
            new Generated.Response4(results.Select(ApiContractMapper.ToContract).ToList()));
    }

    [HttpGet("prize-distribution")]
    [ProducesResponseType(typeof(Generated.Response5), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.Response5>> GetPrizeDistribution(
        string eventId,
        CancellationToken cancellationToken)
    {
        var distribution = await dispatcher.Send(
            new GetPrizeDistributionQuery(ApiContractMapper.ParseGuid(eventId, "eventId")),
            cancellationToken);

        return Ok(new Generated.Response5(
            distribution.Rows.Select(ApiContractMapper.ToContract).ToList(),
            distribution.Stars));
    }
}
