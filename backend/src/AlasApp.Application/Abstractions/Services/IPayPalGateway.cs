namespace AlasApp.Application.Abstractions.Services;

public interface IPayPalGateway
{
    Task<PayPalCreateOrderResult> CreateOrderAsync(Guid inscriptionId, decimal amountUsd, string returnUrl, string cancelUrl, CancellationToken cancellationToken);
    Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, CancellationToken cancellationToken);
}

public sealed record PayPalCreateOrderResult(string OrderId, string ApprovalUrl);
public sealed record PayPalCaptureResult(string OrderId, string CaptureId, decimal AmountUsd);
