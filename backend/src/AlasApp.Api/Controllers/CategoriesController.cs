using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Categories.Commands.DeleteCategory;
using AlasApp.Application.Categories.Queries.GetCategoryById;
using AlasApp.Application.Categories.Queries.ListCategories;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/categories")]
public sealed class CategoriesController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Generated.Response6), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.Response6>> List([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new ListCategoriesQuery(new Application.Categories.Models.CategoryListFilter(ApiContractMapper.ParseCategoryStatus(status))),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("{categoryId}")]
    [ProducesResponseType(typeof(Generated.CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.CategoryResponse>> GetById(string categoryId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new GetCategoryByIdQuery(ApiContractMapper.ParseGuid(categoryId, "categoryId")),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Generated.CategoryResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<Generated.CategoryResponse>> Create([FromBody] Generated.CategoryRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(body), cancellationToken);
        var contract = ApiContractMapper.ToContract(result);

        return CreatedAtAction(nameof(GetById), new { categoryId = contract.Id }, contract);
    }

    [HttpPut("{categoryId}")]
    [ProducesResponseType(typeof(Generated.CategoryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.CategoryResponse>> Update(string categoryId, [FromBody] Generated.CategoryRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            ApiContractMapper.ToCommand(ApiContractMapper.ParseGuid(categoryId, "categoryId"), body),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpDelete("{categoryId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string categoryId, CancellationToken cancellationToken)
    {
        await dispatcher.Send(new DeleteCategoryCommand(ApiContractMapper.ParseGuid(categoryId, "categoryId")), cancellationToken);
        return NoContent();
    }
}
