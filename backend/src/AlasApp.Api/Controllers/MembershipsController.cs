using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Memberships.Models;
using AlasApp.Application.Memberships.Queries.GetMembershipById;
using AlasApp.Application.Memberships.Queries.ListMemberships;
using AlasApp.Application.Memberships.Commands.DeleteMembership;
using Microsoft.AspNetCore.Mvc;
using Generated = AlasApp.AlasApi.Api.Controllers;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/memberships")]
public sealed class MembershipsController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Generated.MembershipListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.MembershipListResponse>> List(
        [FromQuery] int? page,
        [FromQuery] int? limit,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var query = new ListMembershipsQuery(
            new MembershipListFilter(
                page ?? 1,
                limit ?? 20,
                ApiContractMapper.ParseMembershipStatus(status)));

        var result = await dispatcher.Send(query, cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("{membershipId}")]
    [ProducesResponseType(typeof(Generated.MembershipResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.MembershipResponse>> GetById(string membershipId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetMembershipByIdQuery(ApiContractMapper.ParseGuid(membershipId, "membershipId")),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Generated.MembershipResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<Generated.MembershipResponse>> Create([FromBody] Generated.MembershipRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(body), cancellationToken);
        var contract = ApiContractMapper.ToContract(result);
        return CreatedAtAction(nameof(GetById), new { membershipId = contract.Id }, contract);
    }

    [HttpPut("{membershipId}")]
    [ProducesResponseType(typeof(Generated.MembershipResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.MembershipResponse>> Update(string membershipId, [FromBody] Generated.MembershipRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            ApiContractMapper.ToCommand(ApiContractMapper.ParseGuid(membershipId, "membershipId"), body),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpDelete("{membershipId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string membershipId, CancellationToken cancellationToken)
    {
        await dispatcher.Send(
            new DeleteMembershipCommand(ApiContractMapper.ParseGuid(membershipId, "membershipId")),
            cancellationToken);

        return NoContent();
    }
}
