using AlasApp.Api.Authorization;
using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Competitors.Queries.GetCompetitorById;
using AlasApp.Application.Events.Queries.GetEventById;
using AlasApp.Domain.Enums;
using AlasApp.Application.EventCategories.Queries.GetEventCategories;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/events/{eventId}/categories")]
public sealed class EventCategoriesController(IRequestDispatcher dispatcher, IInscriptionRepository inscriptionRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Generated.EventCategoryListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.EventCategoryListResponse>> List(
        string eventId,
        [FromQuery] string? competitorId,
        CancellationToken cancellationToken)
    {
        var parsedEventId = ApiContractMapper.ParseGuid(eventId, "eventId");
        var result = await dispatcher.Send(new GetEventCategoriesQuery(parsedEventId), cancellationToken);
        string? existingMembershipPlan = null;

        if (!string.IsNullOrWhiteSpace(competitorId))
        {
            var parsedCompetitorId = ApiContractMapper.ParseGuid(competitorId, "competitorId");
            var competitor = await dispatcher.Send(new GetCompetitorByIdQuery(parsedCompetitorId), cancellationToken);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var age = today.Year - competitor.FechaNacimiento.Year;
            if (today < DateOnly.FromDateTime(competitor.FechaNacimiento.AddYears(age).Date))
            {
                age--;
            }

            var registeredCategoryIds = await inscriptionRepository.ListRegisteredCategoryIdsAsync(
                parsedCompetitorId, parsedEventId, cancellationToken);

            var filtered = result.Data
                .Where(x => IsGenderCompatible(competitor.Genero, x.Gender))
                .Where(x => !x.AgeRestriction || (x.MinAge <= age && x.MaxAge >= age))
                .Where(x => !registeredCategoryIds.Contains(x.CategoryId))
                .ToList();
            result = result with { Data = filtered };

            var eventDto = await dispatcher.Send(new GetEventByIdQuery(parsedEventId), cancellationToken);
            var activePlan = await inscriptionRepository.GetActiveMembershipPlanForEventAsync(
                parsedCompetitorId, parsedEventId, eventDto.CircuitId, cancellationToken);
            existingMembershipPlan = activePlan?.ToString();
        }

        var contract = ApiContractMapper.ToContract(result);
        if (existingMembershipPlan is not null)
        {
            contract.AdditionalProperties["existingMembershipPlan"] = existingMembershipPlan;
        }

        return Ok(contract);
    }

    [HttpPut]
    [Authorize(Policy = AdminPolicies.EventsWrite)]
    [ProducesResponseType(typeof(Generated.EventCategoryListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.EventCategoryListResponse>> Update(string eventId, [FromBody] Generated.Body body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            ApiContractMapper.ToCommand(ApiContractMapper.ParseGuid(eventId, "eventId"), body),
            cancellationToken);

        return Ok(ApiContractMapper.ToUpdatedEventCategoriesContract(result));
    }

    private static bool IsGenderCompatible(CompetitorGender competitorGender, CategoryGender categoryGender)
    {
        return categoryGender == CategoryGender.Ambos
            || (categoryGender == CategoryGender.Masculino && competitorGender == CompetitorGender.Masculino)
            || (categoryGender == CategoryGender.Femenino && competitorGender == CompetitorGender.Femenino);
    }
}
