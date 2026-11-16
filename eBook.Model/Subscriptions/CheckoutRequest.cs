using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Subscriptions;

public class CheckoutRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Plan pretplate nije ispravan.")]
    public int PlanId { get; set; }
}
