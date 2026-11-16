using System.Linq.Expressions;
using eBook.Model.Common;
using eBook.Model.ReadingProgress;
using eBook.Services.Database;
using eBook.Services.Extensions;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class ReadingProgressService : IReadingProgressService
{
    private static readonly Expression<Func<ReadingProgress, ReadingProgressResponse>> ToResponse = progress => new ReadingProgressResponse
    {
        BookId = progress.BookId,
        Title = progress.Book.Title,
        HasCover = progress.Book.CoverImage != null,
        CurrentPage = progress.CurrentPage,
        TotalPages = progress.Book.PageCount,
        IsFinished = progress.IsFinished,
        LastReadAt = progress.LastReadAt
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IBookService _bookService;

    public ReadingProgressService(ApplicationDbContext db, ICurrentUserService currentUser, IBookService bookService)
    {
        _db = db;
        _currentUser = currentUser;
        _bookService = bookService;
    }

    public Task<PagedResult<ReadingProgressResponse>> GetAsync(PagedRequest request)
    {
        var userId = _currentUser.UserId;

        return _db.ReadingProgresses
            .AsNoTracking()
            .Where(progress => progress.UserId == userId && progress.Book.IsActive)
            .OrderByDescending(progress => progress.LastReadAt)
            .Select(ToResponse)
            .ToPagedResultAsync(request);
    }

    public Task<ReadingProgressResponse?> GetByBookAsync(int bookId)
    {
        var userId = _currentUser.UserId;

        return _db.ReadingProgresses
            .AsNoTracking()
            .Where(progress => progress.UserId == userId && progress.BookId == bookId)
            .Select(ToResponse)
            .FirstOrDefaultAsync();
    }

    public async Task<ReadingProgressResponse> UpsertAsync(ReadingProgressUpsertRequest request)
    {
        var userId = _currentUser.UserId;

        // Ista provjera kao pri citanju: knjiga mora postojati, stranica mora postojati i Premium zahtijeva pretplatu.
        var page = await _bookService.GetPageAsync(request.BookId, request.CurrentPage);

        var progress = await _db.ReadingProgresses
            .SingleOrDefaultAsync(p => p.UserId == userId && p.BookId == request.BookId);

        if (progress is null)
        {
            progress = new ReadingProgress { UserId = userId, BookId = request.BookId };
            _db.ReadingProgresses.Add(progress);
        }

        progress.CurrentPage = request.CurrentPage;
        progress.IsFinished = progress.IsFinished || request.CurrentPage >= page.TotalPages;
        progress.LastReadAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (await GetByBookAsync(request.BookId))!;
    }
}
