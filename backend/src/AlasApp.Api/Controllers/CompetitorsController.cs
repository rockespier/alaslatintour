using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Auth.Commands.ChangeUserPassword;
using AlasApp.Application.CompetitorFines.Commands.CreateCompetitorFine;
using AlasApp.Application.CompetitorFines.Commands.UpdateCompetitorFine;
using AlasApp.Application.CompetitorFines.Queries.ListCompetitorFines;
using AlasApp.Application.Competitors.Commands.DeleteCompetitor;
using AlasApp.Application.Competitors.Commands.UpdateCompetitorLicense;
using AlasApp.Application.Competitors.Commands.UpdateCompetitorNotifications;
using AlasApp.Application.Competitors.Queries.GetCompetitorCalendar;
using AlasApp.Application.Competitors.Queries.GetCompetitorById;
using AlasApp.Application.Competitors.Queries.GetCompetitorIdentityDocument;
using AlasApp.Application.Competitors.Queries.GetCompetitorInscriptions;
using AlasApp.Application.Competitors.Queries.GetCompetitorNotifications;
using AlasApp.Application.Competitors.Queries.GetCompetitorPointsHistory;
using AlasApp.Application.Competitors.Queries.ListCompetitors;
using AlasApp.Application.Abstractions.Persistence;
using Generated = AlasApp.AlasApi.Api.Controllers;
using AlasApp.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/competitors")]
public sealed class CompetitorsController(IRequestDispatcher dispatcher, IUserAccountRepository userAccountRepository) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AdminPolicies.UsersRead)]
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
    [Authorize(Policy = AdminPolicies.UsersRead)]
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
    [Authorize(Policy = AdminPolicies.UsersWrite)]
    [ProducesResponseType(typeof(Generated.CompetitorResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<Generated.CompetitorResponse>> Create([FromBody] Generated.CompetitorRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(body), cancellationToken);
        var contract = ApiContractMapper.ToContract(result);

        return CreatedAtAction(nameof(GetById), new { competitorId = contract.Id }, contract);
    }

    [HttpPut("{competitorId}")]
    [Authorize(Policy = AdminPolicies.UsersWrite)]
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
    [Authorize(Policy = AdminPolicies.UsersWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string competitorId, CancellationToken cancellationToken)
    {
        await dispatcher.Send(new DeleteCompetitorCommand(ApiContractMapper.ParseGuid(competitorId, "competitorId")), cancellationToken);
        return NoContent();
    }

    [HttpPut("{competitorId}/license")]
    [Authorize(Policy = AdminPolicies.UsersWrite)]
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

    [HttpGet("{competitorId}/identity-document")]
    [Authorize(Policy = AdminPolicies.UsersRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIdentityDocument(string competitorId, CancellationToken cancellationToken)
    {
        var document = await dispatcher.Send(
            new GetCompetitorIdentityDocumentQuery(ApiContractMapper.ParseGuid(competitorId, "competitorId")),
            cancellationToken);

        return File(document.Content, document.ContentType, document.FileName);
    }

    [HttpPost("{competitorId}/password")]
    [Authorize(Policy = AdminPolicies.UsersWrite)]
    [ProducesResponseType(typeof(Generated.MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.MessageResponse>> ChangePassword(
        string competitorId,
        [FromBody] PasswordChangeRequest body,
        CancellationToken cancellationToken)
    {
        var competitorGuid = ApiContractMapper.ParseGuid(competitorId, "competitorId");
        var userAccount = await userAccountRepository.GetByCompetitorIdAsync(competitorGuid, cancellationToken)
            ?? throw new AlasApp.Application.Common.NotFoundException("No existe una cuenta de usuario vinculada a este competidor.");

        await dispatcher.Send(new ChangeUserPasswordCommand(userAccount.Id, body.NewPassword), cancellationToken);
        return Ok(new Generated.MessageResponse("Contraseña actualizada correctamente."));
    }

    [HttpGet("{competitorId}/fines")]
    [Authorize(Policy = AdminPolicies.PaymentsRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<CompetitorFineResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CompetitorFineResponse>>> ListFines(
        string competitorId,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new ListCompetitorFinesQuery(ApiContractMapper.ParseGuid(competitorId, "competitorId")),
            cancellationToken);

        return Ok(result.Select(ApiContractMapper.ToContract).ToList());
    }

    [HttpPost("{competitorId}/fines")]
    [Authorize(Policy = AdminPolicies.PaymentsWrite)]
    [ProducesResponseType(typeof(CompetitorFineResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CompetitorFineResponse>> CreateFine(
        string competitorId,
        [FromBody] CompetitorFineRequest body,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new CreateCompetitorFineCommand(
                ApiContractMapper.ParseGuid(competitorId, "competitorId"),
                body.AmountUsd,
                body.Reason,
                body.Notes,
                ResolveCurrentUserId(User)),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ApiContractMapper.ToContract(result));
    }

    [HttpPut("{competitorId}/fines/{fineId}")]
    [Authorize(Policy = AdminPolicies.PaymentsWrite)]
    [ProducesResponseType(typeof(CompetitorFineResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompetitorFineResponse>> UpdateFine(
        string competitorId,
        string fineId,
        [FromBody] CompetitorFineUpdateRequest body,
        CancellationToken cancellationToken)
    {
        _ = ApiContractMapper.ParseGuid(competitorId, "competitorId");

        var result = await dispatcher.Send(
            new UpdateCompetitorFineCommand(
                ApiContractMapper.ParseGuid(fineId, "fineId"),
                body.AmountUsd,
                body.Reason,
                body.Notes,
                body.Status),
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

    private static Guid ResolveCurrentUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException("No se pudo identificar el usuario autenticado.");
        }

        return userId;
    }
}
