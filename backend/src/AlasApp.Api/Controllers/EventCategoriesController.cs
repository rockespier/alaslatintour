using AlasApp.Api.Authorization;
using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Competitors.Queries.GetCompetitorById;
using AlasApp.Domain.Enums;
using AlasApp.Application.EventCategories.Queries.GetEventCategories;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/events/{eventId}/categories")]
public sealed class EventCategoriesController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Generated.EventCategoryListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.EventCategoryListResponse>> List(
        string eventId,
        [FromQuery] string? competitorId,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetEventCategoriesQuery(ApiContractMapper.ParseGuid(eventId, "eventId")),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(competitorId))
        {
            var competitor = await dispatcher.Send(
                new GetCompetitorByIdQuery(ApiContractMapper.ParseGuid(competitorId, "competitorId")),
                cancellationToken);

            var filtered = result.Data.Where(x => IsGenderCompatible(competitor.Genero, x.Gender)).ToList();
            result = result with { Data = filtered };
        }

        return Ok(ApiContractMapper.ToContract(result));
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
