using eBook.Model.Enums;

namespace eBook.Services.Database;

public class Subscription
{
    public int Id { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Audit trag promjena statusa.
    public DateTime? StatusChangedAt { get; set; }
    public string? StatusChangeNote { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int SubscriptionPlanId { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
