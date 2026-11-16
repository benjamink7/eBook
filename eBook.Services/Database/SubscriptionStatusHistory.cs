using eBook.Model.Enums;

namespace eBook.Services.Database;

// Audit trail: svaka promjena statusa pretplate ostaje zabiljezena (ko, kada, zasto).
public class SubscriptionStatusHistory
{
    public int Id { get; set; }
    public SubscriptionStatus? FromStatus { get; set; }
    public SubscriptionStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; }

    // Null znaci da je promjenu izvrsio sistem (npr. Stripe webhook).
    public int? ChangedByUserId { get; set; }
    public User? ChangedByUser { get; set; }

    public int SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;
}
