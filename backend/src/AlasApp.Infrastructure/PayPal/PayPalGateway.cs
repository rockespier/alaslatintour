using AlasApp.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace AlasApp.Infrastructure.PayPal;

public sealed class PayPalGateway(
    HttpClient httpClient,
    IOptionsMonitor<PayPalOptions> options,
    PayPalTokenCache tokenCache,
    ILogger<PayPalGateway> logger)
    : IPayPalGateway
{
    public async Task<PayPalCreateOrderResult> CreateOrderAsync(
        Guid inscriptionId,
        decimal amountUsd,
        string returnUrl,
        string cancelUrl,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);

        var body = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = inscriptionId.ToString(),
                    amount = new
                    {
                        currency_code = "USD",
                        value = amountUsd.ToString("0.00", CultureInfo.InvariantCulture)
                    }
                }
            },
            payment_source = new
            {
                paypal = new
                {
                    experience_context = new
                    {
                        payment_method_preference = "IMMEDIATE_PAYMENT_REQUIRED",
                        landing_page = "LOGIN",
                        shipping_preference = "NO_SHIPPING",
                        user_action = "PAY_NOW",
                        return_url = returnUrl,
                        cancel_url = cancelUrl
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "v2/checkout/orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(body);

        var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        var orderId = json.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("PayPal no retorno un orderId.");

        var approvalUrl = json.GetProperty("links")
            .EnumerateArray()
            .FirstOrDefault(l => l.GetProperty("rel").GetString() == "payer-action")
            .GetProperty("href").GetString()
            ?? throw new InvalidOperationException("PayPal no retorno una URL de aprobacion.");

        logger.LogInformation("PayPal order created: {OrderId}", orderId);
        return new PayPalCreateOrderResult(orderId, approvalUrl);
    }

    public async Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"v2/checkout/orders/{orderId}/capture");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        var responseOrderId = json.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("PayPal no retorno el orderId en la captura.");

        var capture = json.GetProperty("purchase_units")[0]
            .GetProperty("payments")
            .GetProperty("captures")[0];

        var captureId = capture.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("PayPal no retorno un captureId.");

        var amountStr = capture.GetProperty("amount").GetProperty("value").GetString() ?? "0";
        var amount = decimal.Parse(amountStr, CultureInfo.InvariantCulture);

        logger.LogInformation("PayPal order captured: {OrderId} / capture {CaptureId}", responseOrderId, captureId);
        return new PayPalCaptureResult(responseOrderId, captureId, amount);
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        return await tokenCache.GetOrRefreshAsync(async ct =>
        {
            var opts = options.CurrentValue;
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{opts.ClientId}:{opts.ClientSecret}"));

            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/oauth2/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(
                [new KeyValuePair<string, string>("grant_type", "client_credentials")]);

            var response = await httpClient.SendAsync(request, ct);
            await EnsureSuccessAsync(response, ct);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var accessToken = json.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("PayPal no retorno un access_token.");
            var expiresIn = json.GetProperty("expires_in").GetInt32();

            return (accessToken, expiresIn);
        }, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"PayPal API error {(int)response.StatusCode}: {body}",
                null,
                response.StatusCode);
        }
    }
}
