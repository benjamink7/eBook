using eBook.Model.Common;
using eBook.Model.Exceptions;
using eBook.Model.Genres;
using eBook.Services.Database;
using eBook.Services.Extensions;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class GenreService : IGenreService
{
    private readonly ApplicationDbContext _db;

    public GenreService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<GenreResponse>> GetAsync(GenreSearchRequest request)
    {
        var query = _db.Genres.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(genre => genre.Name.Contains(term));
        }

        return query
            .OrderBy(genre => genre.Name)
            .Select(genre => new GenreResponse { Id = genre.Id, Name = genre.Name, Description = genre.Description })
            .ToPagedResultAsync(request);
    }

    public async Task<GenreResponse> GetByIdAsync(int id)
    {
        return await _db.Genres.AsNoTracking()
            .Where(genre => genre.Id == id)
            .Select(genre => new GenreResponse { Id = genre.Id, Name = genre.Name, Description = genre.Description })
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException("Žanr nije pronađen.");
    }

    public async Task<GenreResponse> CreateAsync(GenreUpsertRequest request)
    {
        var name = request.Name.Trim();
        await EnsureNameIsUniqueAsync(name, null);

        var genre = new Genre { Name = name, Description = request.Description?.Trim() };
        _db.Genres.Add(genre);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(genre.Id);
    }

    public async Task<GenreResponse> UpdateAsync(int id, GenreUpsertRequest request)
    {
        var genre = await _db.Genres.SingleOrDefaultAsync(g => g.Id == id)
            ?? throw new NotFoundException("Žanr nije pronađen.");

        var name = request.Name.Trim();
        await EnsureNameIsUniqueAsync(name, id);

        genre.Name = name;
        genre.Description = request.Description?.Trim();
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var genre = await _db.Genres.SingleOrDefaultAsync(g => g.Id == id)
            ?? throw new NotFoundException("Žanr nije pronađen.");

        var isUsed = await _db.BookGenres.AnyAsync(bg => bg.GenreId == id)
            || await _db.SearchHistories.AnyAsync(history => history.GenreId == id);

        if (isUsed)
        {
            throw new BusinessException("Žanr nije moguće obrisati jer je povezan s knjigama ili historijom pretraga.");
        }

        _db.Genres.Remove(genre);
        await _db.SaveChangesAsync();
    }

    private async Task EnsureNameIsUniqueAsync(string name, int? currentId)
    {
        if (await _db.Genres.AnyAsync(g => g.Name == name && g.Id != currentId))
        {
            throw new BusinessException("Žanr s tim nazivom već postoji.");
        }
    }
}
