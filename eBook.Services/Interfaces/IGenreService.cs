using eBook.Model.Common;
using eBook.Model.Genres;

namespace eBook.Services.Interfaces;

public interface IGenreService
{
    Task<PagedResult<GenreResponse>> GetAsync(GenreSearchRequest request);
    Task<GenreResponse> GetByIdAsync(int id);
    Task<GenreResponse> CreateAsync(GenreUpsertRequest request);
    Task<GenreResponse> UpdateAsync(int id, GenreUpsertRequest request);
    Task DeleteAsync(int id);
}
