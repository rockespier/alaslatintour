using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Videos.Models;

namespace AlasApp.Application.Videos.Queries.ListVideos;

public sealed class ListVideosQueryHandler(IVideoService videoService)
    : IRequestHandler<ListVideosQuery, IReadOnlyCollection<VideoDto>>
{
    public Task<IReadOnlyCollection<VideoDto>> Handle(ListVideosQuery request, CancellationToken cancellationToken)
        => videoService.ListAsync(cancellationToken);
}
