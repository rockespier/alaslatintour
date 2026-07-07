using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.CategoryTariffs.Commands.UpsertCategoryTariff;
using AlasApp.Application.CategoryTariffs.Queries.GetCategoryTariffs;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/categories/{categoryId}/tariffs")]
public sealed class CategoryTariffsController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Generated.Response7), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.Response7>> List(string categoryId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetCategoryTariffsQuery(ApiContractMapper.ParseGuid(categoryId, "categoryId")),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPut("{starLevel:int}")]
    [ProducesResponseType(typeof(Generated.TariffResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.TariffResponse>> Upsert(
        string categoryId,
        int starLevel,
        [FromBody] Generated.TariffRequest body,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new UpsertCategoryTariffCommand(
                ApiContractMapper.ParseGuid(categoryId, "categoryId"),
                starLevel,
                (decimal)body.Usd,
                (decimal)body.Cop,
                body.Active),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }
}
