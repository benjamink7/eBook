namespace eBook.Services.Database;

public class SubscriptionPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "eur";
    public int DurationInMonths { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
