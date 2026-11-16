using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Recommendations;

public class RecommendationRequest
{
    [Range(1, 30, ErrorMessage = "Broj preporuka mora biti između 1 i 30.")]
    public int Count { get; set; } = 10;
}
