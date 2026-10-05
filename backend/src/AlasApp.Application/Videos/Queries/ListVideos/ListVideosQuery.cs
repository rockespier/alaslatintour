using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Videos.Models;

namespace AlasApp.Application.Videos.Queries.ListVideos;

public sealed record ListVideosQuery : IRequest<IReadOnlyCollection<VideoDto>>;
