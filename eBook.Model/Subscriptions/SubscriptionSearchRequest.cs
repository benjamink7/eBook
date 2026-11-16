using eBook.Model.Common;
using eBook.Model.Enums;

namespace eBook.Model.Subscriptions;

public class SubscriptionSearchRequest : PagedRequest
{
    public SubscriptionStatus? Status { get; set; }
    public string? Search { get; set; }
}
