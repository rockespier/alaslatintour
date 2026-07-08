using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Articles.Commands.DeleteArticle;
using AlasApp.Application.Articles.Queries.GetArticleBySlug;
using AlasApp.Application.Articles.Queries.ListArticles;
using AlasApp.Application.Articles.Models;
using Generated = AlasApp.AlasApi.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/articles")]
public sealed class ArticlesController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Generated.ArticleListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Generated.ArticleListResponse>> List(
        [FromQuery] int? page,
        [FromQuery] int? limit,
        [FromQuery] string? category,
        [FromQuery] bool? featured,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new ListArticlesQuery(
                new ArticleListFilter(
                    page ?? 1,
                    limit ?? 20,
                    ApiContractMapper.ParseArticleCategory(category),
                    featured,
                    search)),
            cancellationToken);

        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(Generated.ArticleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.ArticleResponse>> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetArticleBySlugQuery(slug), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Generated.ArticleResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<Generated.ArticleResponse>> Create([FromBody] Generated.ArticleRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(body), cancellationToken);
        var contract = ApiContractMapper.ToContract(result);
        return CreatedAtAction(nameof(GetBySlug), new { slug = contract.Slug }, contract);
    }

    [HttpPut("{slug}")]
    [ProducesResponseType(typeof(Generated.ArticleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Generated.ArticleResponse>> Update(string slug, [FromBody] Generated.ArticleRequest body, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(ApiContractMapper.ToCommand(slug, body), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpDelete("{slug}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string slug, CancellationToken cancellationToken)
    {
        await dispatcher.Send(new DeleteArticleCommand(slug), cancellationToken);
        return NoContent();
    }
}
