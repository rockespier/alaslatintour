using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AlasApp.Api.Models;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed record PageSectionResponse(string Title, IReadOnlyList<string> Paragraphs);

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed record PageContentResponse(
    string Slug,
    string Lang,
    string Title,
    IReadOnlyList<string> Intro,
    IReadOnlyList<PageSectionResponse> Sections);
