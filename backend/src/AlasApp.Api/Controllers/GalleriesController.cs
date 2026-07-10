using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Galleries.Queries.GetGalleryBySlug;
using AlasApp.Application.Galleries.Queries.ListGalleries;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/galleries")]
public sealed class GalleriesController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GalleryListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GalleryListResponse>> List(CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new ListGalleriesQuery(), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(GalleryDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GalleryDetailResponse>> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetGalleryBySlugQuery(slug), cancellationToken);
        return Ok(ApiContractMapper.ToContract(result));
    }
}
