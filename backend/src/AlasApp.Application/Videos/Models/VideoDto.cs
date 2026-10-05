namespace AlasApp.Application.Videos.Models;

public sealed record VideoDto(
    string Id,
    string Title,
    DateTimeOffset PublishedAt,
    string ThumbnailUrl,
    string Url);
