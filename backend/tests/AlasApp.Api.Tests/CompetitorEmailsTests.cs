using AlasApp.Application.Emails;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Enums;
using Xunit;

namespace AlasApp.Api.Tests;

public sealed class CompetitorEmailsTests
{
    [Theory]
    [InlineData(PreferredLanguage.English, "en")]
    [InlineData(PreferredLanguage.Portugues, "pt")]
    [InlineData(PreferredLanguage.Espanol, "es")]
    [InlineData(null, "es")]
    public void EmailLanguage_From_ShouldMapPreferredLanguage(PreferredLanguage? preferred, string expected)
    {
        Assert.Equal(expected, EmailLanguage.From(preferred));
    }

    [Theory]
    [InlineData("en", "Payment confirmed — Roca Bruja", "Official notification", "Amount paid")]
    [InlineData("pt", "Pagamento confirmado — Roca Bruja", "Notificação oficial", "Valor pago")]
    [InlineData("es", "Pago confirmado — Roca Bruja", "Notificacion oficial", "Monto pagado")]
    public void PaymentConfirmed_ShouldUseRequestedLanguage(string lang, string subject, string chrome, string amountLabel)
    {
        var email = CompetitorEmails.PaymentConfirmed(lang, "Ana", "Roca Bruja", "Open Damas", 45m);

        Assert.Equal(subject, email.Subject);
        // The template HTML-encodes accents (ç → &#231;), so assert on the decoded markup.
        var html = System.Net.WebUtility.HtmlDecode(email.Html);
        Assert.Contains($"<html lang=\"{lang}\">", html);
        Assert.Contains(chrome, html);
        Assert.Contains(amountLabel, html);
        Assert.Contains("USD 45", email.Text);
    }

    [Fact]
    public void BeachTokenApproved_ShouldTranslateChromeButKeepAdminInstructions()
    {
        const string instructions = "Hola Ana, tu token para Roca Bruja es ABCD-1234.";
        var token = new BeachTokenAdminDto(
            Guid.NewGuid(), "Ana Ruiz", "ana@test.com", "Roca Bruja", "Open Damas",
            45m, 45m, 0m, 0m, "ABCD-1234", TokenHistoryStatus.Pendiente, DateTimeOffset.UtcNow, null, null);

        var email = CompetitorEmails.BeachTokenApproved("en", token, instructions);

        Assert.Equal("Payment token for Roca Bruja", email.Subject);
        Assert.Equal(instructions, email.Text);
        Assert.Contains("Your payment token was approved", email.Html);
        Assert.Contains("Valid until", email.Html);
    }
}
