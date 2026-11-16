using eBook.Model.Recommendations;

namespace eBook.Services.Interfaces;

public interface IRecommendationService
{
    Task<RecommendationsResponse> GetAsync(RecommendationRequest request);
}
