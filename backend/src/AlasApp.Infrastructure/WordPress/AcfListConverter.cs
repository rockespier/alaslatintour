using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlasApp.Infrastructure.WordPress;

/// <summary>
/// ACF serializes an empty gallery/repeater field as <c>""</c> or <c>false</c> instead of <c>[]</c>.
/// Reads any non-array value as an empty list so one empty day doesn't break the whole payload.
/// </summary>
internal sealed class AcfListConverter<T> : JsonConverter<IReadOnlyList<T>?>
{
    public override IReadOnlyList<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            reader.Skip();
            return [];
        }

        return JsonSerializer.Deserialize<List<T>>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyList<T>? value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, options);
}
