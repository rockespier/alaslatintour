using AlasApp.Api.Authorization;
using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Inscriptions.Models;
using AlasApp.Application.Inscriptions.Queries.ListInscriptions;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/events/{eventId}/inscriptions")]
public sealed class EventInscriptionsController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AdminPolicies.InscriptionsRead)]
    [ProducesResponseType(typeof(Generated.AdminInscriptionListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.AdminInscriptionListResponse>> List(
        string eventId,
        [FromQuery] int? page,
        [FromQuery] int? limit,
        [FromQuery] string? categoryId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new ListInscriptionsQuery(
                new AdminInscriptionListFilter(
                    page ?? 1,
                    limit ?? 20,
                    ApiContractMapper.ParseGuid(eventId, "eventId"),
                    ApiContractMapper.ParseOptionalGuid(categoryId, "categoryId"),
                    ApiContractMapper.ParseInscriptionStatusAdmin(status))),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }
}
