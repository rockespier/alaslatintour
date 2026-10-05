using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AlasApp.Api.Models;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed record VideoResponse(string Id, string Title, DateTimeOffset PublishedAt, string ThumbnailUrl, string Url);

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed record VideoListResponse(IReadOnlyCollection<VideoResponse> Data);
