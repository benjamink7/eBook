using eBook.Model.Common;
using eBook.Model.Countries;
using eBook.Model.Exceptions;
using eBook.Services.Database;
using eBook.Services.Extensions;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class CountryService : ICountryService
{
    private readonly ApplicationDbContext _db;

    public CountryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<CountryResponse>> GetAsync(CountrySearchRequest request)
    {
        var query = _db.Countries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(country => country.Name.Contains(term));
        }

        return query
            .OrderBy(country => country.Name)
            .Select(country => new CountryResponse
            {
                Id = country.Id,
                Name = country.Name,
                CityCount = country.Cities.Count
            })
            .ToPagedResultAsync(request);
    }

    public async Task<CountryResponse> GetByIdAsync(int id)
    {
        return await _db.Countries.AsNoTracking()
            .Where(country => country.Id == id)
            .Select(country => new CountryResponse
            {
                Id = country.Id,
                Name = country.Name,
                CityCount = country.Cities.Count
            })
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException("Država nije pronađena.");
    }

    public async Task<CountryResponse> CreateAsync(CountryUpsertRequest request)
    {
        var name = request.Name.Trim();
        await EnsureNameIsUniqueAsync(name, null);

        var country = new Country { Name = name };
        _db.Countries.Add(country);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(country.Id);
    }

    public async Task<CountryResponse> UpdateAsync(int id, CountryUpsertRequest request)
    {
        var country = await _db.Countries.SingleOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("Država nije pronađena.");

        var name = request.Name.Trim();
        await EnsureNameIsUniqueAsync(name, id);

        country.Name = name;
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var country = await _db.Countries.SingleOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("Država nije pronađena.");

        if (await _db.Cities.AnyAsync(city => city.CountryId == id))
        {
            throw new BusinessException("Državu nije moguće obrisati jer ima povezane gradove.");
        }

        _db.Countries.Remove(country);
        await _db.SaveChangesAsync();
    }

    private async Task EnsureNameIsUniqueAsync(string name, int? currentId)
    {
        if (await _db.Countries.AnyAsync(c => c.Name == name && c.Id != currentId))
        {
            throw new BusinessException("Država s tim nazivom već postoji.");
        }
    }
}
