using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Rankings.Commands.SyncSurfScoresCircuit;
using AlasApp.Application.Rankings.Queries.GetRanking;
using AlasApp.Application.Rankings.Queries.ListRankingCategories;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1")]
public sealed class RankingsController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet("rankings")]
    [ProducesResponseType(typeof(Generated.RankingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.RankingResponse>> GetRanking(
        [FromQuery] string categoryId,
        [FromQuery] int? year,
        [FromQuery] int? page,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetRankingQuery(ApiContractMapper.ParseGuid(categoryId, "categoryId"), year, page, limit),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("rankings/categories")]
    [ProducesResponseType(typeof(Generated.Response7), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.Response7>> GetCategories(CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new ListRankingCategoriesQuery(), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPost("surfscores/sync/{circuitId}")]
    [ProducesResponseType(typeof(Generated.Response8), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.Response8>> SyncCircuit(string circuitId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new SyncSurfScoresCircuitCommand(ApiContractMapper.ParseGuid(circuitId, "circuitId")),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }
}
