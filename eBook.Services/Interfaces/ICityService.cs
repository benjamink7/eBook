using eBook.Model.Cities;
using eBook.Model.Common;

namespace eBook.Services.Interfaces;

public interface ICityService
{
    Task<PagedResult<CityResponse>> GetAsync(CitySearchRequest request);
    Task<CityResponse> GetByIdAsync(int id);
    Task<CityResponse> CreateAsync(CityUpsertRequest request);
    Task<CityResponse> UpdateAsync(int id, CityUpsertRequest request);
    Task DeleteAsync(int id);
}
