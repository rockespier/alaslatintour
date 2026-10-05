using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Videos.Queries.ListVideos;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/videos")]
public sealed class VideosController(IRequestDispatcher dispatcher) : ControllerBase
{
    /// <summary>Latest videos of the official YouTube channel (cached server-side).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(VideoListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VideoListResponse>> List(CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new ListVideosQuery(), cancellationToken);
        return Ok(new VideoListResponse(
            result.Select(v => new VideoResponse(v.Id, v.Title, v.PublishedAt, v.ThumbnailUrl, v.Url)).ToList()));
    }
}
