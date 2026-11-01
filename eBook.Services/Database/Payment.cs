using eBook.Model.Enums;

namespace eBook.Services.Database;

public class Payment
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public PaymentStatus Status { get; set; }
    public string StripePaymentIntentId { get; set; } = null!;
    public string? StripeRefundId { get; set; }
    public decimal? RefundedAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;
}
