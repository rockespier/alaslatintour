using AlasApp.Api.Authorization;
using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Payments.Commands.ImportMembershipPayments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/events/{eventId}/membership-payments")]
public sealed class EventMembershipPaymentsController(IRequestDispatcher dispatcher, IBulkExcelService bulkExcelService) : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("template")]
    [Authorize(Policy = AdminPolicies.PaymentsWrite)]
    public IActionResult DownloadTemplate(string eventId)
    {
        return File(bulkExcelService.BuildMembershipPaymentsTemplate(), ExcelContentType, "membresias-template.xlsx");
    }

    [HttpPost("import")]
    [Authorize(Policy = AdminPolicies.PaymentsWrite)]
    public async Task<IActionResult> Import(
        string eventId,
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
            new ImportMembershipPaymentsCommand(ApiContractMapper.ParseGuid(eventId, "eventId"), memory.ToArray()),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }
}
