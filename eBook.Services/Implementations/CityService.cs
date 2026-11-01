using eBook.Model.Cities;
using eBook.Model.Common;
using eBook.Services.Database;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class CityService : ICityService
{
    private readonly ApplicationDbContext _db;

    public CityService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<CityResponse>> GetAsync(CitySearchRequest request)
    {
        var query = _db.Cities.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(city => city.Name.Contains(term));
        }

        if (request.CountryId.HasValue)
        {
            query = query.Where(city => city.CountryId == request.CountryId.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(city => city.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(city => new CityResponse
            {
                Id = city.Id,
                Name = city.Name,
                CountryName = city.Country.Name
            })
            .ToListAsync();

        return new PagedResult<CityResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
