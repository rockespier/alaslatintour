using AlasApp.Api.Authorization;
using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Inscriptions.Commands.ImportInscriptions;
using AlasApp.Application.Inscriptions.Models;
using AlasApp.Application.Inscriptions.Queries.ListConfirmedInscriptions;
using AlasApp.Application.Inscriptions.Queries.ListInscriptions;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/events/{eventId}/inscriptions")]
public sealed class EventInscriptionsController(IRequestDispatcher dispatcher, IBulkExcelService bulkExcelService) : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";


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

    /// <summary>
    /// Roster público de inscritos con pago confirmado. Sin datos sensibles (sin montos, sin ids internos).
    /// </summary>
    [HttpGet("confirmed")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ConfirmedInscriptionRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<ConfirmedInscriptionRowDto>>> ListConfirmed(
        string eventId,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new ListConfirmedInscriptionsQuery(ApiContractMapper.ParseGuid(eventId, "eventId")),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("template")]
    [Authorize(Policy = AdminPolicies.InscriptionsWrite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DownloadTemplate(string eventId)
    {
        return File(bulkExcelService.BuildInscriptionsTemplate(), ExcelContentType, "inscripciones-template.xlsx");
    }

    [HttpPost("import")]
    [Authorize(Policy = AdminPolicies.InscriptionsWrite)]
    [ProducesResponseType(typeof(BulkImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BulkImportResponse>> Import(
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
            new ImportInscriptionsCommand(
                ApiContractMapper.ParseGuid(eventId, "eventId"),
                ApiContractMapper.ParseGuid(categoryId, "categoryId"),
                memory.ToArray()),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }
}
