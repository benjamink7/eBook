using eBook.Model.Enums;

namespace eBook.Model.Subscriptions;

public class SubscriptionResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string PlanName { get; set; } = null!;
    public SubscriptionStatus Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
}
