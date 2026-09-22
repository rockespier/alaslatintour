using AlasApp.Api.Authorization;
using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Payments.Queries.GetPaymentById;
using AlasApp.Application.Payments.Queries.GetPaymentKpis;
using AlasApp.Application.Payments.Queries.ListMembershipPayments;
using AlasApp.Application.Payments.Queries.ListPayments;
using AlasApp.Application.Payments.Models;
using AlasApp.Application.Abstractions.Services;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/payments")]
public sealed class PaymentsController(IRequestDispatcher dispatcher, IBulkExcelService bulkExcelService) : ControllerBase
{
    private const int MaxExportRows = 5000;
    [HttpGet]
    [Authorize(Policy = AdminPolicies.PaymentsRead)]
    [ProducesResponseType(typeof(Generated.PaymentListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.PaymentListResponse>> List(
        [FromQuery] int? page,
        [FromQuery] int? limit,
        [FromQuery] string? method,
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new ListPaymentsQuery(
                new PaymentListFilter(
                    page ?? 1,
                    limit ?? 20,
                    ApiContractMapper.ParsePaymentMethod(method),
                    ApiContractMapper.ParsePaymentStatusAdmin(status),
                    fromDate,
                    toDate)),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("export")]
    [Authorize(Policy = AdminPolicies.PaymentsRead)]
    public async Task<IActionResult> Export(
        [FromQuery] string? method, [FromQuery] string? status,
        [FromQuery] DateTimeOffset? fromDate, [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new ListPaymentsQuery(new PaymentListFilter(
            1, MaxExportRows, ApiContractMapper.ParsePaymentMethod(method),
            ApiContractMapper.ParsePaymentStatusAdmin(status), fromDate, toDate)), cancellationToken);
        return File(bulkExcelService.BuildPaymentsExport(result.Items),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "transacciones.xlsx");
    }

    [HttpGet("{paymentId}")]
    [Authorize(Policy = AdminPolicies.PaymentsRead)]
    [ProducesResponseType(typeof(Generated.PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.PaymentResponse>> GetById(string paymentId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetPaymentByIdQuery(ApiContractMapper.ParseGuid(paymentId, "paymentId")),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Generated.PaymentResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<Generated.PaymentResponse>> Create([FromBody] Generated.PaymentRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(body), cancellationToken);
        var contract = ApiContractMapper.ToContract(result);
        return CreatedAtAction(nameof(GetById), new { paymentId = contract.Id }, contract);
    }

    [HttpPut("{paymentId}")]
    [Authorize(Policy = AdminPolicies.PaymentsWrite)]
    [ProducesResponseType(typeof(Generated.PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.PaymentResponse>> Update(
        string paymentId,
        [FromBody] Generated.PaymentUpdateRequest body,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            ApiContractMapper.ToCommand(ApiContractMapper.ParseGuid(paymentId, "paymentId"), body),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("kpis")]
    [Authorize(Policy = AdminPolicies.PaymentsRead)]
    [ProducesResponseType(typeof(Generated.PaymentKpiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.PaymentKpiResponse>> GetKpis(CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetPaymentKpisQuery(), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("memberships")]
    [Authorize(Policy = AdminPolicies.PaymentsRead)]
    public async Task<IActionResult> ListMembershipPayments(
        [FromQuery] int? page,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new ListMembershipPaymentsQuery(page ?? 1, limit ?? 50), cancellationToken);

        return Ok(new
        {
            data = result.Items.Select(x => new
            {
                id = x.PaymentId,
                competitorName = x.CompetitorName,
                paymentDate = x.PaymentDate,
                membershipPlan = x.MembershipPlan.ToString(),
                eventName = x.EventName,
                circuitName = x.CircuitName,
                method = x.Method.ToString(),
                amountUsd = x.AmountUsd
            }),
            pagination = new
            {
                currentPage = result.CurrentPage,
                itemsPerPage = result.ItemsPerPage,
                totalItems = result.TotalItems,
                totalPages = result.TotalPages
            }
        });
    }
}
