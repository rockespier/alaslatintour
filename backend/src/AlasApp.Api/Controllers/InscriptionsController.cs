using AlasApp.Api.Authorization;
using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Inscriptions.Commands.DeleteInscription;
using AlasApp.Application.Inscriptions.Queries.GetInscriptionById;
using AlasApp.Application.Inscriptions.Queries.ListInscriptions;
using AlasApp.Application.Inscriptions.Models;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/inscriptions")]
public sealed class InscriptionsController(IRequestDispatcher dispatcher, IBulkExcelService bulkExcelService) : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const int MaxExportRows = 5000;

    [HttpGet("export")]
    [Authorize(Policy = AdminPolicies.InscriptionsRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery] string? eventId,
        [FromQuery] string? categoryId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new ListInscriptionsQuery(
                new AdminInscriptionListFilter(
                    1,
                    MaxExportRows,
                    ApiContractMapper.ParseOptionalGuid(eventId, "eventId"),
                    ApiContractMapper.ParseOptionalGuid(categoryId, "categoryId"),
                    ApiContractMapper.ParseInscriptionStatusAdmin(status))),
            cancellationToken);

        var content = bulkExcelService.BuildInscriptionsExport(result.Items);
        return File(content, ExcelContentType, "inscritos.xlsx");
    }

    [HttpGet("{inscriptionId}/export")]
    [Authorize(Policy = AdminPolicies.InscriptionsRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportFicha(string inscriptionId, CancellationToken cancellationToken)
    {
        var inscription = await dispatcher.Send(
            new GetInscriptionByIdQuery(ApiContractMapper.ParseGuid(inscriptionId, "inscriptionId")),
            cancellationToken);

        var content = bulkExcelService.BuildInscriptionFicha(inscription);
        return File(content, ExcelContentType, $"ficha-{inscription.Id}.xlsx");
    }

    [HttpGet]
    [Authorize(Policy = AdminPolicies.InscriptionsRead)]
    [ProducesResponseType(typeof(Generated.AdminInscriptionListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.AdminInscriptionListResponse>> List(
        [FromQuery] int? page,
        [FromQuery] int? limit,
        [FromQuery] string? eventId,
        [FromQuery] string? categoryId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new ListInscriptionsQuery(
                new AdminInscriptionListFilter(
                    page ?? 1,
                    limit ?? 20,
                    ApiContractMapper.ParseOptionalGuid(eventId, "eventId"),
                    ApiContractMapper.ParseOptionalGuid(categoryId, "categoryId"),
                    ApiContractMapper.ParseInscriptionStatusAdmin(status))),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("{inscriptionId}")]
    [ProducesResponseType(typeof(Generated.InscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.InscriptionResponse>> GetById(string inscriptionId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetInscriptionByIdQuery(ApiContractMapper.ParseGuid(inscriptionId, "inscriptionId")),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Generated.InscriptionResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<Generated.InscriptionResponse>> Create([FromBody] Generated.InscriptionRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(body), cancellationToken);
        var contract = ApiContractMapper.ToContract(result);
        return CreatedAtAction(nameof(GetById), new { inscriptionId = contract.Id }, contract);
    }

    [HttpPut("{inscriptionId}")]
    [Authorize(Policy = AdminPolicies.InscriptionsWrite)]
    [ProducesResponseType(typeof(Generated.InscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.InscriptionResponse>> Update(
        string inscriptionId,
        [FromBody] Generated.InscriptionUpdateRequest body,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            ApiContractMapper.ToCommand(ApiContractMapper.ParseGuid(inscriptionId, "inscriptionId"), body),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpDelete("{inscriptionId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string inscriptionId, CancellationToken cancellationToken)
    {
        await dispatcher.Send(
            new DeleteInscriptionCommand(
                ApiContractMapper.ParseGuid(inscriptionId, "inscriptionId"),
                ResolveCurrentCompetitorId(User)),
            cancellationToken);

        return NoContent();
    }

    private static Guid ResolveCurrentCompetitorId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("competitor_id");
        if (!Guid.TryParse(value, out var competitorId))
        {
            throw new UnauthorizedAccessException("No se pudo identificar el competidor autenticado.");
        }

        return competitorId;
    }
}
