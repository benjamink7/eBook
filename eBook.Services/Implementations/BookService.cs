using System.Linq.Expressions;
using eBook.Model.Books;
using eBook.Model.Common;
using eBook.Model.Enums;
using eBook.Model.Exceptions;
using eBook.Model.Genres;
using eBook.Services.Books;
using eBook.Services.Database;
using eBook.Services.Extensions;
using eBook.Services.Files;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class BookService : IBookService
{
    public const int MaxCoverSizeBytes = 2 * 1024 * 1024;
    private const int MaxCharactersPerPage = 1800;

    public static readonly Expression<Func<Book, BookListItemResponse>> ListItemProjection = book => new BookListItemResponse
    {
        Id = book.Id,
        Title = book.Title,
        AuthorName = book.Author.FirstName + " " + book.Author.LastName,
        PublishedYear = book.PublishedYear,
        AccessType = book.AccessType,
        IsActive = book.IsActive,
        HasCover = book.CoverImage != null,
        Genres = book.BookGenres.Select(bg => bg.Genre.Name).OrderBy(name => name).ToList(),
        AverageRating = book.Reviews.Average(review => (double?)review.Rating),
        ReviewCount = book.Reviews.Count
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BookService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<BookListItemResponse>> GetAsync(BookSearchRequest request)
    {
        var query = _db.Books.AsNoTracking().AsQueryable();

        // Obicni korisnici vide samo aktivne knjige.
        if (!_currentUser.IsAdmin)
        {
            query = query.Where(book => book.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(book =>
                book.Title.Contains(term)
                || book.Author.FirstName.Contains(term)
                || book.Author.LastName.Contains(term)
                || (book.Author.FirstName + " " + book.Author.LastName).Contains(term));
        }

        if (request.GenreId.HasValue)
        {
            query = query.Where(book => book.BookGenres.Any(bg => bg.GenreId == request.GenreId.Value));
        }

        if (request.AuthorId.HasValue)
        {
            query = query.Where(book => book.AuthorId == request.AuthorId.Value);
        }

        if (request.AccessType.HasValue)
        {
            query = query.Where(book => book.AccessType == request.AccessType.Value);
        }

        var ordered = request.SortBy switch
        {
            BookSortBy.Newest => query.OrderByDescending(book => book.CreatedAt).ThenBy(book => book.Title),
            BookSortBy.Rating => query
                .OrderByDescending(book => book.Reviews.Average(review => (double?)review.Rating))
                .ThenBy(book => book.Title),
            _ => query.OrderBy(book => book.Title)
        };

        await RecordSearchAsync(request);

        return await ordered.Select(ListItemProjection).ToPagedResultAsync(request);
    }

    public async Task<BookDetailsResponse> GetByIdAsync(int id)
    {
        var book = await _db.Books.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BookDetailsResponse
            {
                Id = b.Id,
                Title = b.Title,
                Description = b.Description,
                PublishedYear = b.PublishedYear,
                PageCount = b.PageCount,
                AccessType = b.AccessType,
                IsActive = b.IsActive,
                AuthorId = b.AuthorId,
                AuthorName = b.Author.FirstName + " " + b.Author.LastName,
                HasCover = b.CoverImage != null,
                Genres = b.BookGenres
                    .Select(bg => new GenreResponse { Id = bg.GenreId, Name = bg.Genre.Name, Description = bg.Genre.Description })
                    .ToList(),
                AverageRating = b.Reviews.Average(review => (double?)review.Rating),
                ReviewCount = b.Reviews.Count
            })
            .SingleOrDefaultAsync();

        if (book is null || (!book.IsActive && !_currentUser.IsAdmin))
        {
            throw new NotFoundException("Knjiga nije pronađena.");
        }

        book.CanRead = book.AccessType == BookAccessType.Free || await HasPremiumAccessAsync();

        return book;
    }

    public async Task<BookDetailsResponse> CreateAsync(BookUpsertRequest request)
    {
        await ValidateReferencesAsync(request);

        var pages = SplitContent(request.Content)
            ?? throw new BusinessException("Unesite sadržaj knjige.");

        var book = new Book
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            PublishedYear = request.PublishedYear,
            AccessType = request.AccessType,
            AuthorId = request.AuthorId,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            PageCount = pages.Count,
            BookGenres = request.GenreIds.Distinct().Select(genreId => new BookGenre { GenreId = genreId }).ToList(),
            Pages = pages.Select((content, index) => new BookPage { PageNumber = index + 1, Content = content }).ToList()
        };

        _db.Books.Add(book);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(book.Id);
    }

    public async Task<BookDetailsResponse> UpdateAsync(int id, BookUpsertRequest request)
    {
        var book = await _db.Books.Include(b => b.BookGenres).SingleOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException("Knjiga nije pronađena.");

        await ValidateReferencesAsync(request);

        book.Title = request.Title.Trim();
        book.Description = request.Description.Trim();
        book.PublishedYear = request.PublishedYear;
        book.AccessType = request.AccessType;
        book.AuthorId = request.AuthorId;
        book.IsActive = request.IsActive;

        var wantedGenreIds = request.GenreIds.Distinct().ToHashSet();
        _db.BookGenres.RemoveRange(book.BookGenres.Where(bg => !wantedGenreIds.Contains(bg.GenreId)).ToList());

        foreach (var genreId in wantedGenreIds.Where(genreId => book.BookGenres.All(bg => bg.GenreId != genreId)))
        {
            book.BookGenres.Add(new BookGenre { GenreId = genreId });
        }

        var pages = SplitContent(request.Content);

        if (pages is null)
        {
            await _db.SaveChangesAsync();
        }
        else
        {
            // Zamjena stranica i izmjena knjige moraju proci zajedno ili nijedna.
            await using var transaction = await _db.Database.BeginTransactionAsync();

            await _db.BookPages.Where(page => page.BookId == id).ExecuteDeleteAsync();

            book.PageCount = pages.Count;
            _db.BookPages.AddRange(pages.Select((content, index) =>
                new BookPage { BookId = id, PageNumber = index + 1, Content = content }));

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var book = await _db.Books.SingleOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException("Knjiga nije pronađena.");

        var isUsed = await _db.Reviews.AnyAsync(review => review.BookId == id)
            || await _db.Favorites.AnyAsync(favorite => favorite.BookId == id)
            || await _db.ReadingProgresses.AnyAsync(progress => progress.BookId == id);

        if (isUsed)
        {
            throw new BusinessException(
                "Knjigu nije moguće obrisati jer su uz nju vezane ocjene, favoriti ili historija čitanja. Umjesto brisanja možete je deaktivirati.");
        }

        _db.Books.Remove(book);
        await _db.SaveChangesAsync();
    }

    public async Task<(byte[] Content, string ContentType)> GetCoverAsync(int id)
    {
        var isAdmin = _currentUser.IsAdmin;

        var cover = await _db.Books.AsNoTracking()
            .Where(book => book.Id == id && (book.IsActive || isAdmin))
            .Select(book => book.CoverImage)
            .SingleOrDefaultAsync();

        if (cover is null)
        {
            throw new NotFoundException("Naslovnica nije pronađena.");
        }

        return (cover, ImageValidator.DetectContentType(cover) ?? "application/octet-stream");
    }

    public async Task SetCoverAsync(int id, byte[] content)
    {
        if (content.Length == 0)
        {
            throw new BusinessException("Odaberite sliku naslovnice.");
        }

        if (content.Length > MaxCoverSizeBytes)
        {
            throw new BusinessException("Slika naslovnice može imati najviše 2 MB.");
        }

        if (ImageValidator.DetectContentType(content) is null)
        {
            throw new BusinessException("Dozvoljeni formati slike su PNG i JPEG.");
        }

        var book = await _db.Books.SingleOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException("Knjiga nije pronađena.");

        book.CoverImage = content;
        await _db.SaveChangesAsync();
    }

    public async Task<BookPageResponse> GetPageAsync(int id, int pageNumber)
    {
        var isAdmin = _currentUser.IsAdmin;

        var book = await _db.Books.AsNoTracking()
            .Where(b => b.Id == id && (b.IsActive || isAdmin))
            .Select(b => new { b.AccessType, b.PageCount })
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException("Knjiga nije pronađena.");

        // Provjera se radi na serveru; klijent ne moze zaobici Premium ogranicenje.
        if (book.AccessType == BookAccessType.Premium && !await HasPremiumAccessAsync())
        {
            throw new ForbiddenException("Ova knjiga je dostupna samo uz Premium pretplatu.");
        }

        if (pageNumber < 1 || pageNumber > book.PageCount)
        {
            throw new NotFoundException("Tražena stranica ne postoji.");
        }

        var content = await _db.BookPages.AsNoTracking()
            .Where(page => page.BookId == id && page.PageNumber == pageNumber)
            .Select(page => page.Content)
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException("Tražena stranica ne postoji.");

        return new BookPageResponse
        {
            BookId = id,
            PageNumber = pageNumber,
            TotalPages = book.PageCount,
            Content = content
        };
    }

    // Pretrage obicnih korisnika se pamte; to su stvarni podaci za preporuke.
    private async Task RecordSearchAsync(BookSearchRequest request)
    {
        if (_currentUser.IsAdmin || request.Page != 1 || string.IsNullOrWhiteSpace(request.Search))
        {
            return;
        }

        var term = request.Search.Trim();
        var genreExists = request.GenreId.HasValue && await _db.Genres.AnyAsync(genre => genre.Id == request.GenreId.Value);

        _db.SearchHistories.Add(new SearchHistory
        {
            UserId = _currentUser.UserId,
            Query = term.Length > 200 ? term[..200] : term,
            GenreId = genreExists ? request.GenreId : null,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    private async Task<bool> HasPremiumAccessAsync()
    {
        if (_currentUser.IsAdmin)
        {
            return true;
        }

        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        return await _db.Subscriptions.AnyAsync(subscription =>
            subscription.UserId == userId
            && subscription.Status == SubscriptionStatus.Active
            && subscription.EndDate > now);
    }

    private async Task ValidateReferencesAsync(BookUpsertRequest request)
    {
        if (!await _db.Authors.AnyAsync(author => author.Id == request.AuthorId))
        {
            throw new BusinessException("Odabrani autor ne postoji.");
        }

        var genreIds = request.GenreIds.Distinct().ToList();

        if (await _db.Genres.CountAsync(genre => genreIds.Contains(genre.Id)) != genreIds.Count)
        {
            throw new BusinessException("Jedan ili više odabranih žanrova ne postoji.");
        }
    }

    private static List<string>? SplitContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var pages = BookPaginator.Split(content, MaxCharactersPerPage);

        return pages.Count == 0 ? null : pages;
    }
}
