using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AlasApp.Api.Models;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed record UploadedMediaResponse(
    string MediaId,
    string Url,
    string FileName,
    string ContentType,
    long SizeBytes);
