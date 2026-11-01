using eBook.Model.Cities;
using eBook.Model.Common;

namespace eBook.Services.Interfaces;

public interface ICityService
{
    Task<PagedResult<CityResponse>> GetAsync(CitySearchRequest request);
}
