namespace AlasApp.Application.Common;

/// <summary>
/// Languages the public site is published in. Used to ask WordPress (WPML/Polylang)
/// for editorial content in the visitor's language via the standard <c>?lang=xx</c> parameter.
/// </summary>
public static class ContentLanguage
{
    private static readonly string[] Supported = ["es", "en", "pt"];

    /// <summary>Returns <c>es</c>, <c>en</c> or <c>pt</c>; anything else (or nothing) yields <c>null</c>.</summary>
    public static string? Normalize(string? value)
    {
        var lang = value?.Trim().ToLowerInvariant();
        return lang is not null && Supported.Contains(lang) ? lang : null;
    }
}
