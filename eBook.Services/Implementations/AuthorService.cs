using eBook.Model.Authors;
using eBook.Model.Common;
using eBook.Model.Exceptions;
using eBook.Services.Database;
using eBook.Services.Extensions;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class AuthorService : IAuthorService
{
    private readonly ApplicationDbContext _db;

    public AuthorService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<AuthorResponse>> GetAsync(AuthorSearchRequest request)
    {
        var query = _db.Authors.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(author =>
                author.FirstName.Contains(term)
                || author.LastName.Contains(term)
                || (author.FirstName + " " + author.LastName).Contains(term));
        }

        return query
            .OrderBy(author => author.LastName)
            .ThenBy(author => author.FirstName)
            .Select(ToResponse)
            .ToPagedResultAsync(request);
    }

    public async Task<AuthorResponse> GetByIdAsync(int id)
    {
        return await _db.Authors.AsNoTracking()
            .Where(author => author.Id == id)
            .Select(ToResponse)
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException("Autor nije pronađen.");
    }

    public async Task<AuthorResponse> CreateAsync(AuthorUpsertRequest request)
    {
        var author = new Author();
        Apply(author, request);

        _db.Authors.Add(author);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(author.Id);
    }

    public async Task<AuthorResponse> UpdateAsync(int id, AuthorUpsertRequest request)
    {
        var author = await _db.Authors.SingleOrDefaultAsync(a => a.Id == id)
            ?? throw new NotFoundException("Autor nije pronađen.");

        Apply(author, request);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var author = await _db.Authors.SingleOrDefaultAsync(a => a.Id == id)
            ?? throw new NotFoundException("Autor nije pronađen.");

        if (await _db.Books.AnyAsync(book => book.AuthorId == id))
        {
            throw new BusinessException("Autora nije moguće obrisati jer su uz njega vezane knjige.");
        }

        _db.Authors.Remove(author);
        await _db.SaveChangesAsync();
    }

    private static void Apply(Author author, AuthorUpsertRequest request)
    {
        author.FirstName = request.FirstName.Trim();
        author.LastName = request.LastName.Trim();
        author.Biography = request.Biography?.Trim();
        author.BirthYear = request.BirthYear;
    }

    private static readonly System.Linq.Expressions.Expression<Func<Author, AuthorResponse>> ToResponse =
        author => new AuthorResponse
        {
            Id = author.Id,
            FirstName = author.FirstName,
            LastName = author.LastName,
            Biography = author.Biography,
            BirthYear = author.BirthYear,
            BookCount = author.Books.Count
        };
}
