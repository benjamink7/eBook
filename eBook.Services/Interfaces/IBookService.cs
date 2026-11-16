using eBook.Model.Books;
using eBook.Model.Common;

namespace eBook.Services.Interfaces;

public interface IBookService
{
    Task<PagedResult<BookListItemResponse>> GetAsync(BookSearchRequest request);
    Task<BookDetailsResponse> GetByIdAsync(int id);
    Task<BookDetailsResponse> CreateAsync(BookUpsertRequest request);
    Task<BookDetailsResponse> UpdateAsync(int id, BookUpsertRequest request);
    Task DeleteAsync(int id);
    Task<(byte[] Content, string ContentType)> GetCoverAsync(int id);
    Task SetCoverAsync(int id, byte[] content);
    Task<BookPageResponse> GetPageAsync(int id, int pageNumber);
}
