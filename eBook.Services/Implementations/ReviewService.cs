using eBook.Model.Common;
using eBook.Model.Exceptions;
using eBook.Model.Reviews;
using eBook.Services.Database;
using eBook.Services.Extensions;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ReviewService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<PagedResult<ReviewResponse>> GetAsync(ReviewSearchRequest request)
    {
        var userId = _currentUser.UserId;

        return _db.Reviews
            .AsNoTracking()
            .Where(review => review.BookId == request.BookId)
            .OrderByDescending(review => review.CreatedAt)
            .Select(review => new ReviewResponse
            {
                Id = review.Id,
                BookId = review.BookId,
                Username = review.User.Username,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                IsMine = review.UserId == userId
            })
            .ToPagedResultAsync(request);
    }

    public async Task<ReviewResponse> UpsertAsync(ReviewUpsertRequest request)
    {
        var userId = _currentUser.UserId;

        var bookExists = await _db.Books.AnyAsync(book => book.Id == request.BookId && book.IsActive);

        if (!bookExists)
        {
            throw new NotFoundException("Knjiga nije pronađena.");
        }

        // Ocjene su stvarni podaci za preporuke, pa ocjenjivati moze samo ko je knjigu zaista poceo citati.
        var hasStartedReading = await _db.ReadingProgresses
            .AnyAsync(progress => progress.UserId == userId && progress.BookId == request.BookId);

        if (!hasStartedReading)
        {
            throw new BusinessException("Knjigu možete ocijeniti tek nakon što je počnete čitati.");
        }

        var review = await _db.Reviews.SingleOrDefaultAsync(r => r.UserId == userId && r.BookId == request.BookId);

        if (review is null)
        {
            review = new Review { UserId = userId, BookId = request.BookId, CreatedAt = DateTime.UtcNow };
            _db.Reviews.Add(review);
        }

        review.Rating = request.Rating;
        review.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        await _db.SaveChangesAsync();

        var username = await _db.Users.Where(user => user.Id == userId).Select(user => user.Username).SingleAsync();

        return new ReviewResponse
        {
            Id = review.Id,
            BookId = review.BookId,
            Username = username,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt,
            IsMine = true
        };
    }

    public async Task DeleteAsync(int id)
    {
        var userId = _currentUser.UserId;
        var isAdmin = _currentUser.IsAdmin;

        var review = await _db.Reviews.SingleOrDefaultAsync(r => r.Id == id && (isAdmin || r.UserId == userId))
            ?? throw new NotFoundException("Recenzija nije pronađena.");

        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();
    }
}
