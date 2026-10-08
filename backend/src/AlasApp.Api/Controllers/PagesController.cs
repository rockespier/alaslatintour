using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Common;
using AlasApp.Application.Pages.Queries.GetPageBySlug;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/pages")]
public sealed class PagesController(IRequestDispatcher dispatcher) : ControllerBase
{
    /// <summary>Editable text of a static page from WordPress (e.g. `quienes-somos`), as plain-text paragraphs.</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(PageContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PageContentResponse>> GetBySlug(string slug, [FromQuery] string? lang, CancellationToken cancellationToken)
    {
        var page = await dispatcher.Send(new GetPageBySlugQuery(slug, ContentLanguage.Normalize(lang)), cancellationToken);
        return Ok(new PageContentResponse(
            page.Slug,
            page.Lang,
            page.Title,
            page.Intro,
            page.Sections.Select(s => new PageSectionResponse(s.Title, s.Paragraphs)).ToList()));
    }
}
