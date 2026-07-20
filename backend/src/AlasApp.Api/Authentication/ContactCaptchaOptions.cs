namespace AlasApp.Api.Authentication;

public sealed class ContactCaptchaOptions
{
    public const string SectionName = "ContactCaptcha";

    public bool Enabled { get; init; } = true;

    public string SecretKey { get; init; } = string.Empty;
}
