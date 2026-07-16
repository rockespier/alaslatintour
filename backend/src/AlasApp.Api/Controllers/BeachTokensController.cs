using AlasApp.Api.Authorization;
using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Payments.Commands.ApproveBeachToken;
using AlasApp.Application.Payments.Commands.RequestBeachToken;
using AlasApp.Application.Payments.Commands.RedeemBeachToken;
using AlasApp.Application.Payments.Queries.ListBeachTokens;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/payments/beach")]
public sealed class BeachTokensController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpPost("request")]
    [ProducesResponseType(typeof(Generated.BeachTokenPendingResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<Generated.BeachTokenPendingResponse>> RequestToken(
        [FromBody] Generated.BeachTokenRequest body,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToBeachTokenRequestCommand(body), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiContractMapper.ToContract(result));
    }

    [HttpPost("redeem")]
    [ProducesResponseType(typeof(Generated.BeachTokenRedeemResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.BeachTokenRedeemResponse>> Redeem(
        [FromBody] Generated.BeachTokenRedeemRequest body,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToBeachTokenRedeemCommand(body), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("tokens")]
    [Authorize(Policy = AdminPolicies.TokensRead)]
    [ProducesResponseType(typeof(Generated.BeachTokenAdminListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.BeachTokenAdminListResponse>> ListTokens(
        [FromQuery] int? page,
        [FromQuery] int? limit,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new ListBeachTokensQuery(page ?? 1, limit ?? 20, ApiContractMapper.ParseTokenHistoryStatus(status)),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPost("tokens/{tokenId}/approve")]
    [Authorize(Policy = AdminPolicies.TokensWrite)]
    [ProducesResponseType(typeof(Generated.BeachTokenAdminResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.BeachTokenAdminResponse>> Approve(
        string tokenId,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new ApproveBeachTokenCommand(ApiContractMapper.ParseGuid(tokenId, "tokenId")),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPost("tokens/{tokenId}/reject")]
    [Authorize(Policy = AdminPolicies.TokensWrite)]
    [ProducesResponseType(typeof(Generated.BeachTokenAdminResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.BeachTokenAdminResponse>> Reject(
        string tokenId,
        [FromBody] Generated.Body4 body,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            ApiContractMapper.ToRejectBeachTokenCommand(ApiContractMapper.ParseGuid(tokenId, "tokenId"), body),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }
}
