using AlasApp.Application.Videos.Models;

namespace AlasApp.Application.Abstractions.Services;

public interface IVideoService
{
    /// <summary>Latest videos of the official channel, newest first.</summary>
    Task<IReadOnlyCollection<VideoDto>> ListAsync(CancellationToken cancellationToken);
}
