using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Payments.Commands.CapturePayPalOrder;
using AlasApp.Application.Payments.Commands.InitiatePayPalOrder;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/paypal")]
public sealed class PayPalController(IRequestDispatcher dispatcher) : ControllerBase
{
    public sealed record InitiateOrderRequest(Guid InscriptionId, string ReturnUrl, string CancelUrl);
    public sealed record CaptureOrderRequest(Guid InscriptionId);
    public sealed record OrderResponse(string OrderId, string ApprovalUrl, decimal AmountUsd);

    [HttpPost("orders")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Initiate(
        [FromBody] InitiateOrderRequest body,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new InitiatePayPalOrderCommand(body.InscriptionId, body.ReturnUrl, body.CancelUrl),
            cancellationToken);

        return Ok(new OrderResponse(result.OrderId, result.ApprovalUrl, result.AmountUsd));
    }

    [HttpPost("orders/{orderId}/capture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(
        string orderId,
        [FromBody] CaptureOrderRequest body,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new CapturePayPalOrderCommand(body.InscriptionId, orderId),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }
}
