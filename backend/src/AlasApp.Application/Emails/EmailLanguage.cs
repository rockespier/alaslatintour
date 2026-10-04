using AlasApp.Domain.Enums;

namespace AlasApp.Application.Emails;

/// <summary>Language codes used by transactional emails (<c>es</c>, <c>en</c>, <c>pt</c>).</summary>
public static class EmailLanguage
{
    public const string Spanish = "es";
    public const string English = "en";
    public const string Portuguese = "pt";

    /// <summary>Maps the account's <see cref="PreferredLanguage"/>; unknown or missing falls back to Spanish.</summary>
    public static string From(PreferredLanguage? preferredLanguage) => preferredLanguage switch
    {
        PreferredLanguage.English => English,
        PreferredLanguage.Portugues => Portuguese,
        _ => Spanish,
    };

    /// <summary>Picks the text for <paramref name="lang"/>, falling back to Spanish.</summary>
    public static string Pick(string lang, string es, string en, string pt) => lang switch
    {
        English => en,
        Portuguese => pt,
        _ => es,
    };
}

public sealed record EmailContent(string Subject, string Text, string Html);
