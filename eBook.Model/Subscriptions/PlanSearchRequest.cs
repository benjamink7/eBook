using eBook.Model.Common;

namespace eBook.Model.Subscriptions;

public class PlanSearchRequest : PagedRequest
{
    public string? Search { get; set; }
}
