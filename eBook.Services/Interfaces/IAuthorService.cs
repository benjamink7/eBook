using eBook.Model.Authors;
using eBook.Model.Common;

namespace eBook.Services.Interfaces;

public interface IAuthorService
{
    Task<PagedResult<AuthorResponse>> GetAsync(AuthorSearchRequest request);
    Task<AuthorResponse> GetByIdAsync(int id);
    Task<AuthorResponse> CreateAsync(AuthorUpsertRequest request);
    Task<AuthorResponse> UpdateAsync(int id, AuthorUpsertRequest request);
    Task DeleteAsync(int id);
}
