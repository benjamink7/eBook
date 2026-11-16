using eBook.Model.Books;
using eBook.Model.Common;

namespace eBook.Services.Interfaces;

public interface IFavoriteService
{
    Task<PagedResult<BookListItemResponse>> GetAsync(PagedRequest request);
    Task AddAsync(int bookId);
    Task RemoveAsync(int bookId);
}
