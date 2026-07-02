using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Circuits.Commands.DeleteCircuit;
using AlasApp.Application.Circuits.Models;
using AlasApp.Application.Circuits.Queries.GetCircuitById;
using AlasApp.Application.Circuits.Queries.ListCircuits;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/circuits")]
public sealed class CircuitsController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Generated.CircuitListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.CircuitListResponse>> List(
        [FromQuery] int? page,
        [FromQuery] int? limit,
        [FromQuery] string? status,
        [FromQuery] int? year,
        [FromQuery] string? modalidad,
        CancellationToken cancellationToken)
    {
        var query = new ListCircuitsQuery(
            new CircuitListFilter(
                page ?? 1,
                limit ?? 20,
                ApiContractMapper.ParseCircuitStatus(status),
                year,
                ApiContractMapper.ParseCircuitModalidad(modalidad)));

        var result = await dispatcher.Send(query, cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("{circuitId}")]
    [ProducesResponseType(typeof(Generated.CircuitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.CircuitResponse>> GetById(string circuitId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetCircuitByIdQuery(ApiContractMapper.ParseGuid(circuitId, "circuitId")),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Generated.CircuitResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<Generated.CircuitResponse>> Create([FromBody] Generated.CircuitRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(body), cancellationToken);
        var contract = ApiContractMapper.ToContract(result);

        return CreatedAtAction(nameof(GetById), new { circuitId = contract.Id }, contract);
    }

    [HttpPut("{circuitId}")]
    [ProducesResponseType(typeof(Generated.CircuitResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.CircuitResponse>> Update(string circuitId, [FromBody] Generated.CircuitRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            ApiContractMapper.ToCommand(ApiContractMapper.ParseGuid(circuitId, "circuitId"), body),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpDelete("{circuitId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string circuitId, CancellationToken cancellationToken)
    {
        await dispatcher.Send(
            new DeleteCircuitCommand(ApiContractMapper.ParseGuid(circuitId, "circuitId")),
            cancellationToken);

        return NoContent();
    }
}
