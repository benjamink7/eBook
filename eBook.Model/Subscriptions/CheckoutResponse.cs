namespace eBook.Model.Subscriptions;

public class CheckoutResponse
{
    public int SubscriptionId { get; set; }
    public string PaymentIntentClientSecret { get; set; } = null!;
    public string PublishableKey { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
}
