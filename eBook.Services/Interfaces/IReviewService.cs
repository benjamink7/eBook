using eBook.Model.Common;
using eBook.Model.Reviews;

namespace eBook.Services.Interfaces;

public interface IReviewService
{
    Task<PagedResult<ReviewResponse>> GetAsync(ReviewSearchRequest request);
    Task<ReviewResponse> UpsertAsync(ReviewUpsertRequest request);
    Task DeleteAsync(int id);
}
