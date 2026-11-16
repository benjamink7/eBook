using eBook.Model.Books;

namespace eBook.Model.Recommendations;

public class RecommendationResponse
{
    public BookListItemResponse Book { get; set; } = null!;
    public double Score { get; set; }
    public double ContentScore { get; set; }
    public double CollaborativeScore { get; set; }
    public double PopularityScore { get; set; }

    // Razlozi preporuke napisani za krajnjeg korisnika.
    public List<string> Reasons { get; set; } = new();
}
