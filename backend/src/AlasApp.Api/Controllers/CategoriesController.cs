using AlasApp.Api.Authorization;
using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Categories.Commands.DeleteCategory;
using AlasApp.Application.Categories.Commands.ImportCategories;
using AlasApp.Application.Categories.Queries.GetCategoryById;
using AlasApp.Application.Categories.Queries.ListCategories;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/categories")]
public sealed class CategoriesController(IRequestDispatcher dispatcher, IBulkExcelService bulkExcelService) : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

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
    [Authorize(Policy = AdminPolicies.CategoriesWrite)]
    [ProducesResponseType(typeof(Generated.CategoryResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<Generated.CategoryResponse>> Create([FromBody] Generated.CategoryRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(body), cancellationToken);
        var contract = ApiContractMapper.ToContract(result);

        return CreatedAtAction(nameof(GetById), new { categoryId = contract.Id }, contract);
    }

    [HttpPut("{categoryId}")]
    [Authorize(Policy = AdminPolicies.CategoriesWrite)]
    [ProducesResponseType(typeof(Generated.CategoryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.CategoryResponse>> Update(string categoryId, [FromBody] Generated.CategoryRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            ApiContractMapper.ToCommand(ApiContractMapper.ParseGuid(categoryId, "categoryId"), body),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpDelete("{categoryId}")]
    [Authorize(Policy = AdminPolicies.CategoriesWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string categoryId, CancellationToken cancellationToken)
    {
        await dispatcher.Send(new DeleteCategoryCommand(ApiContractMapper.ParseGuid(categoryId, "categoryId")), cancellationToken);
        return NoContent();
    }

    [HttpGet("template")]
    [Authorize(Policy = AdminPolicies.CategoriesWrite)]
    public IActionResult DownloadTemplate()
    {
        return File(bulkExcelService.BuildCategoriesTemplate(), ExcelContentType, "categories-template.xlsx");
    }

    [HttpPost("import")]
    [Authorize(Policy = AdminPolicies.CategoriesWrite)]
    [ProducesResponseType(typeof(BulkImportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkImportResponse>> Import([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest("El archivo XLSX no puede estar vacío.");
        }

        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);

        var result = await dispatcher.Send(new ImportCategoriesCommand(memory.ToArray()), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }
}
