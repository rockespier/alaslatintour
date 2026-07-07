using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Competitors.Commands.DeleteCompetitor;
using AlasApp.Application.Competitors.Commands.UpdateCompetitorLicense;
using AlasApp.Application.Competitors.Commands.UpdateCompetitorNotifications;
using AlasApp.Application.Competitors.Queries.GetCompetitorCalendar;
using AlasApp.Application.Competitors.Queries.GetCompetitorById;
using AlasApp.Application.Competitors.Queries.GetCompetitorInscriptions;
using AlasApp.Application.Competitors.Queries.GetCompetitorNotifications;
using AlasApp.Application.Competitors.Queries.GetCompetitorPointsHistory;
using AlasApp.Application.Competitors.Queries.ListCompetitors;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/competitors")]
public sealed class CompetitorsController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Generated.CompetitorListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.CompetitorListResponse>> List(
        [FromQuery] int? page,
        [FromQuery] int? limit,
        [FromQuery] string? country,
        [FromQuery] string? categoryId,
        [FromQuery] string? licenseStatus,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new ListCompetitorsQuery(
                new Application.Competitors.Models.CompetitorListFilter(
                    page ?? 1,
                    limit ?? 20,
                    country,
                    categoryId,
                    ApiContractMapper.ParseLicenseStatus(licenseStatus),
                    search)),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("{competitorId}")]
    [ProducesResponseType(typeof(Generated.CompetitorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.CompetitorResponse>> GetById(string competitorId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetCompetitorByIdQuery(ApiContractMapper.ParseGuid(competitorId, "competitorId")),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Generated.CompetitorResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<Generated.CompetitorResponse>> Create([FromBody] Generated.CompetitorRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(body), cancellationToken);
        var contract = ApiContractMapper.ToContract(result);

        return CreatedAtAction(nameof(GetById), new { competitorId = contract.Id }, contract);
    }

    [HttpPut("{competitorId}")]
    [ProducesResponseType(typeof(Generated.CompetitorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.CompetitorResponse>> Update(
        string competitorId,
        [FromBody] Generated.CompetitorRequest body,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            ApiContractMapper.ToCommand(ApiContractMapper.ParseGuid(competitorId, "competitorId"), body),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpDelete("{competitorId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string competitorId, CancellationToken cancellationToken)
    {
        await dispatcher.Send(new DeleteCompetitorCommand(ApiContractMapper.ParseGuid(competitorId, "competitorId")), cancellationToken);
        return NoContent();
    }

    [HttpPut("{competitorId}/license")]
    [ProducesResponseType(typeof(Generated.CompetitorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.CompetitorResponse>> UpdateLicense(
        string competitorId,
        [FromBody] Generated.Body3 body,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new UpdateCompetitorLicenseCommand(
                ApiContractMapper.ParseGuid(competitorId, "competitorId"),
                ApiContractMapper.ToDomainLicenseStatus(body.Status),
                body.LicenseNumber,
                body.ExpirationDate,
                body.EnabledCategories),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("{competitorId}/notifications")]
    [ProducesResponseType(typeof(Generated.NotificationPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.NotificationPreferencesResponse>> GetNotifications(
        string competitorId,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetCompetitorNotificationsQuery(ApiContractMapper.ParseGuid(competitorId, "competitorId")),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPut("{competitorId}/notifications")]
    [ProducesResponseType(typeof(Generated.NotificationPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.NotificationPreferencesResponse>> UpdateNotifications(
        string competitorId,
        [FromBody] Generated.NotificationPreferencesRequest body,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new UpdateCompetitorNotificationsCommand(
                ApiContractMapper.ParseGuid(competitorId, "competitorId"),
                body.Email,
                body.Push,
                body.Resultados,
                body.Inscripciones),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("{competitorId}/inscriptions")]
    [ProducesResponseType(typeof(Generated.InscriptionListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.InscriptionListResponse>> GetInscriptions(
        string competitorId,
        [FromQuery] Generated.InscriptionStatusCompetitor? status,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetCompetitorInscriptionsQuery(
                ApiContractMapper.ParseGuid(competitorId, "competitorId"),
                status?.ToString()),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("{competitorId}/points-history")]
    [ProducesResponseType(typeof(Generated.PointsHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.PointsHistoryResponse>> GetPointsHistory(
        string competitorId,
        [FromQuery] int? year,
        [FromQuery] string? categoryId,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetCompetitorPointsHistoryQuery(
                ApiContractMapper.ParseGuid(competitorId, "competitorId"),
                year,
                categoryId),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("{competitorId}/calendar")]
    [ProducesResponseType(typeof(Generated.Response8), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.Response8>> GetCalendar(string competitorId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetCompetitorCalendarQuery(ApiContractMapper.ParseGuid(competitorId, "competitorId")),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("{competitorId}/calendar/export")]
    [Produces("text/calendar")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportCalendar(string competitorId, CancellationToken cancellationToken)
    {
        var competitorGuid = ApiContractMapper.ParseGuid(competitorId, "competitorId");
        var calendar = await dispatcher.Send(new GetCompetitorCalendarQuery(competitorGuid), cancellationToken);
        var fileContent = BuildICalendar(calendar);
        return File(Encoding.UTF8.GetBytes(fileContent), "text/calendar", $"competitor-{competitorId}.ics");
    }

    private static string BuildICalendar(IReadOnlyCollection<Application.Competitors.Models.CompetitorCalendarEventDto> events)
    {
        var builder = new StringBuilder()
            .AppendLine("BEGIN:VCALENDAR")
            .AppendLine("VERSION:2.0")
            .AppendLine("PRODID:-//ALAS//Competitor Calendar//EN");

        foreach (var item in events)
        {
            builder
                .AppendLine("BEGIN:VEVENT")
                .AppendLine($"UID:{item.EventId}@alasapp")
                .AppendLine($"DTSTART;VALUE=DATE:{item.FechaInicio:yyyyMMdd}")
                .AppendLine($"DTEND;VALUE=DATE:{item.FechaFin.AddDays(1):yyyyMMdd}")
                .AppendLine($"SUMMARY:{item.Nombre}")
                .AppendLine($"LOCATION:{item.Lugar}")
                .AppendLine("END:VEVENT");
        }

        builder.AppendLine("END:VCALENDAR");
        return builder.ToString();
    }
}
