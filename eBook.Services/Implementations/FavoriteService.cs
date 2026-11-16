using eBook.Model.Books;
using eBook.Model.Common;
using eBook.Model.Exceptions;
using eBook.Services.Database;
using eBook.Services.Extensions;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class FavoriteService : IFavoriteService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public FavoriteService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<PagedResult<BookListItemResponse>> GetAsync(PagedRequest request)
    {
        var userId = _currentUser.UserId;

        return _db.Favorites
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId && favorite.Book.IsActive)
            .OrderByDescending(favorite => favorite.CreatedAt)
            .Select(favorite => favorite.Book)
            .Select(BookService.ListItemProjection)
            .ToPagedResultAsync(request);
    }

    public async Task AddAsync(int bookId)
    {
        var userId = _currentUser.UserId;

        var bookExists = await _db.Books.AnyAsync(book => book.Id == bookId && book.IsActive);

        if (!bookExists)
        {
            throw new NotFoundException("Knjiga nije pronađena.");
        }

        var alreadyFavorite = await _db.Favorites.AnyAsync(f => f.UserId == userId && f.BookId == bookId);

        if (alreadyFavorite)
        {
            return;
        }

        _db.Favorites.Add(new Favorite { UserId = userId, BookId = bookId, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
    }

    public async Task RemoveAsync(int bookId)
    {
        var userId = _currentUser.UserId;

        await _db.Favorites.Where(f => f.UserId == userId && f.BookId == bookId).ExecuteDeleteAsync();
    }
}
