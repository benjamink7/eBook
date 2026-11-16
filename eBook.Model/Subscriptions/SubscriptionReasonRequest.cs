using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Subscriptions;

public class SubscriptionReasonRequest
{
    [Required(ErrorMessage = "Razlog je obavezan.")]
    [StringLength(500, MinimumLength = 3, ErrorMessage = "Razlog mora imati između 3 i 500 znakova.")]
    public string Reason { get; set; } = null!;
}
