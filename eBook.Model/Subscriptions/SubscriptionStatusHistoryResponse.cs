using eBook.Model.Enums;

namespace eBook.Model.Subscriptions;

public class SubscriptionStatusHistoryResponse
{
    public SubscriptionStatus? FromStatus { get; set; }
    public SubscriptionStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = null!;
}
