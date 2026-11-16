using eBook.Model.Common;
using eBook.Model.News;

namespace eBook.Services.Interfaces;

public interface INewsService
{
    Task<PagedResult<NewsResponse>> GetAsync(NewsSearchRequest request);
    Task<NewsResponse> GetByIdAsync(int id);
    Task<NewsResponse> CreateAsync(NewsUpsertRequest request, byte[]? image);
    Task<NewsResponse> UpdateAsync(int id, NewsUpsertRequest request, byte[]? image);
    Task DeleteAsync(int id);
    Task<(byte[] Content, string ContentType)> GetImageAsync(int id);
}
