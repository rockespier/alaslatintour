namespace AlasApp.Infrastructure.PayPal;

public sealed class PayPalOptions
{
    public const string SectionName = "PayPal";
    public string Mode { get; init; } = "sandbox";
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://api-m.sandbox.paypal.com";
}
