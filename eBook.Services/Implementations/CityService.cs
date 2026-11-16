using eBook.Model.Cities;
using eBook.Model.Common;
using eBook.Model.Exceptions;
using eBook.Services.Database;
using eBook.Services.Extensions;
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

    public Task<PagedResult<CityResponse>> GetAsync(CitySearchRequest request)
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

        return query
            .OrderBy(city => city.Name)
            .Select(city => new CityResponse
            {
                Id = city.Id,
                Name = city.Name,
                CountryId = city.CountryId,
                CountryName = city.Country.Name
            })
            .ToPagedResultAsync(request);
    }

    public async Task<CityResponse> GetByIdAsync(int id)
    {
        return await _db.Cities.AsNoTracking()
            .Where(city => city.Id == id)
            .Select(city => new CityResponse
            {
                Id = city.Id,
                Name = city.Name,
                CountryId = city.CountryId,
                CountryName = city.Country.Name
            })
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException("Grad nije pronađen.");
    }

    public async Task<CityResponse> CreateAsync(CityUpsertRequest request)
    {
        var name = request.Name.Trim();
        await ValidateAsync(name, request.CountryId, null);

        var city = new City { Name = name, CountryId = request.CountryId };
        _db.Cities.Add(city);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(city.Id);
    }

    public async Task<CityResponse> UpdateAsync(int id, CityUpsertRequest request)
    {
        var city = await _db.Cities.SingleOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("Grad nije pronađen.");

        var name = request.Name.Trim();
        await ValidateAsync(name, request.CountryId, id);

        city.Name = name;
        city.CountryId = request.CountryId;
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var city = await _db.Cities.SingleOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("Grad nije pronađen.");

        if (await _db.Users.AnyAsync(user => user.CityId == id))
        {
            throw new BusinessException("Grad nije moguće obrisati jer je povezan s korisnicima.");
        }

        _db.Cities.Remove(city);
        await _db.SaveChangesAsync();
    }

    private async Task ValidateAsync(string name, int countryId, int? currentId)
    {
        if (!await _db.Countries.AnyAsync(country => country.Id == countryId))
        {
            throw new BusinessException("Odabrana država ne postoji.");
        }

        if (await _db.Cities.AnyAsync(c => c.Name == name && c.CountryId == countryId && c.Id != currentId))
        {
            throw new BusinessException("Grad s tim nazivom već postoji u odabranoj državi.");
        }
    }
}
