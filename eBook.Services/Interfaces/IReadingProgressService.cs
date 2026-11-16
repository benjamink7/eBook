using eBook.Model.Common;
using eBook.Model.ReadingProgress;

namespace eBook.Services.Interfaces;

public interface IReadingProgressService
{
    Task<PagedResult<ReadingProgressResponse>> GetAsync(PagedRequest request);
    Task<ReadingProgressResponse?> GetByBookAsync(int bookId);
    Task<ReadingProgressResponse> UpsertAsync(ReadingProgressUpsertRequest request);
}
