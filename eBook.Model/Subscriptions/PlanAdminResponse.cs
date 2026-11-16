namespace eBook.Model.Subscriptions;

public class PlanAdminResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = null!;
    public int DurationInMonths { get; set; }
    public bool IsActive { get; set; }
    public int SubscriptionCount { get; set; }
}
