namespace AlasApp.Api.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "AlasApp.Api";

    public string Audience { get; init; } = "AlasApp.Client";

    public string SigningKey { get; init; } = "change-this-signing-key-in-production-at-least-32-characters";

    public int AccessTokenExpirationMinutes { get; init; } = 60;

    public int RememberMeExpirationDays { get; init; } = 30;
}
