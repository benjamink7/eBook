using eBook.Model.Common;
using eBook.Model.Countries;

namespace eBook.Services.Interfaces;

public interface ICountryService
{
    Task<PagedResult<CountryResponse>> GetAsync(CountrySearchRequest request);
    Task<CountryResponse> GetByIdAsync(int id);
    Task<CountryResponse> CreateAsync(CountryUpsertRequest request);
    Task<CountryResponse> UpdateAsync(int id, CountryUpsertRequest request);
    Task DeleteAsync(int id);
}
