namespace eBook.Services.Payments;

public record PaymentIntentInfo(string Id, string Status, long AmountReceivedMinor, string Currency, string? ClientSecret);

public enum PaymentWebhookEventType
{
    PaymentSucceeded = 1,
    PaymentFailed = 2,
    PaymentRefunded = 3
}

public record PaymentWebhookEvent(PaymentWebhookEventType Type, string PaymentIntentId, long AmountMinor, string Currency);

public interface IPaymentGateway
{
    Task<PaymentIntentInfo> CreatePaymentIntentAsync(long amountMinor, string currency, int subscriptionId);

    Task<PaymentIntentInfo> GetPaymentIntentAsync(string paymentIntentId);

    Task CancelPaymentIntentAsync(string paymentIntentId);

    Task<string> RefundAsync(string paymentIntentId, long amountMinor, int paymentId);

    // Provjerava potpis webhook-a; vraca null za dogadjaje koje aplikacija ne obradjuje.
    PaymentWebhookEvent? ParseWebhook(string payload, string signatureHeader);
}
