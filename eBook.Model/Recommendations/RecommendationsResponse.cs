namespace eBook.Model.Recommendations;

public class RecommendationsResponse
{
    public const string PersonalizedStrategy = "Personalized";
    public const string PopularityStrategy = "Popularity";

    public string Strategy { get; set; } = null!;
    public List<RecommendationResponse> Items { get; set; } = new();
}
