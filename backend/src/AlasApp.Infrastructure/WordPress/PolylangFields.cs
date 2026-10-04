using AlasApp.Application.Common;
using System.Text.Json;

namespace AlasApp.Infrastructure.WordPress;

/// <summary>
/// Reads the Polylang REST fields registered in WordPress (see documentacion/wordpress_api.md, section E):
/// <c>lang</c> (language slug, or <c>false</c> when the post type isn't managed by Polylang) and
/// <c>translations</c> (<c>{ "es": "slug-es", "en": "slug-en" }</c>; PHP emits <c>[]</c> when empty).
/// </summary>
internal static class PolylangFields
{
    /// <summary>
    /// Language of a post: the <c>lang</c> field when WordPress exposes it, otherwise the entry of
    /// <c>translations</c> that points to the post's own slug. Null when Polylang doesn't manage it.
    /// </summary>
    public static string? Lang(JsonElement? lang, JsonElement? translations, string slug)
    {
        if (lang is { ValueKind: JsonValueKind.String } element)
        {
            return ContentLanguage.Normalize(element.GetString());
        }

        return Translations(translations)
            .FirstOrDefault(t => string.Equals(t.Value, slug, StringComparison.OrdinalIgnoreCase))
            .Key;
    }

    public static IReadOnlyDictionary<string, string> Translations(JsonElement? value)
    {
        var result = new Dictionary<string, string>();
        if (value is not { ValueKind: JsonValueKind.Object } element)
        {
            return result;
        }

        foreach (var property in element.EnumerateObject())
        {
            var lang = ContentLanguage.Normalize(property.Name);
            var slug = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
            if (lang is not null && !string.IsNullOrWhiteSpace(slug))
            {
                result[lang] = slug;
            }
        }

        return result;
    }
}
