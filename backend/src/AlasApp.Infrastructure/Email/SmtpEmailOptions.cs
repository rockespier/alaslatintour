namespace AlasApp.Infrastructure.Email;

public sealed class SmtpEmailOptions
{
    public const string SectionName = "SmtpEmail";

    public bool Enabled { get; init; }

    public string Host { get; init; } = "smtp.gmail.com";

    public int Port { get; init; } = 587;

    public bool EnableSsl { get; init; } = true;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string FromEmail { get; init; } = string.Empty;

    public string FromName { get; init; } = "ALAS Global Tour";
}
