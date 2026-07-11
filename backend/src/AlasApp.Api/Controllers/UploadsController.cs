using AlasApp.Api.Models;
using AlasApp.Application.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlasApp.Api.Controllers;

[ApiController]
[Route("v1/uploads")]
public sealed class UploadsController(IWordPressService wordPressService) : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    [HttpPost("event-poster")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadedMediaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadedMediaResponse>> UploadEventPoster(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Debes adjuntar un archivo." });
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest(new { message = "Solo se permiten archivos JPG, PNG o WEBP." });
        }

        await using var stream = file.OpenReadStream();
        var result = await wordPressService.UploadMediaAsync(
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ApiContractMapper.ToContract(result));
    }
}
