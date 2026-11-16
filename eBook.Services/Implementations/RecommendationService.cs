using eBook.Model.Recommendations;
using eBook.Services.Database;
using eBook.Services.Interfaces;
using eBook.Services.Recommendations;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class RecommendationService : IRecommendationService
{
    private const int RecentSearchesConsidered = 20;
    private const int HighRatingThreshold = 4;

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RecommendationService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<RecommendationsResponse> GetAsync(RecommendationRequest request)
    {
        var userId = _currentUser.UserId;

        var books = await _db.Books
            .AsNoTracking()
            .Where(book => book.IsActive)
            .Select(book => new
            {
                book.Id,
                book.Title,
                AuthorName = book.Author.FirstName + " " + book.Author.LastName,
                book.AuthorId,
                GenreIds = book.BookGenres.Select(bg => bg.GenreId).ToList(),
                AverageRating = book.Reviews.Average(review => (double?)review.Rating),
                ReviewCount = book.Reviews.Count,
                ReaderCount = book.ReadingProgresses.Count
            })
            .ToListAsync();

        var bookInfos = books
            .Select(book => new BookInfo(
                book.Id, book.Title, book.AuthorName, book.AuthorId, book.GenreIds,
                book.AverageRating, book.ReviewCount, book.ReaderCount))
            .ToList();

        var genreNames = await _db.Genres.AsNoTracking().ToDictionaryAsync(genre => genre.Id, genre => genre.Name);

        var signals = await LoadUserSignalsAsync(userId, bookInfos);
        var othersPositive = await LoadOtherUsersPositiveBooksAsync(userId);

        var outcome = RecommendationEngine.Recommend(
            new RecommendationInput(bookInfos, genreNames, signals, othersPositive), request.Count);

        var bookIds = outcome.Items.Select(item => item.BookId).ToList();

        var listItems = await _db.Books
            .AsNoTracking()
            .Where(book => bookIds.Contains(book.Id))
            .Select(BookService.ListItemProjection)
            .ToDictionaryAsync(item => item.Id);

        return new RecommendationsResponse
        {
            Strategy = outcome.IsPersonalized
                ? RecommendationsResponse.PersonalizedStrategy
                : RecommendationsResponse.PopularityStrategy,
            Items = outcome.Items.Select(item => new RecommendationResponse
            {
                Book = listItems[item.BookId],
                Score = Math.Round(item.Score, 3),
                ContentScore = Math.Round(item.Content, 3),
                CollaborativeScore = Math.Round(item.Collaborative, 3),
                PopularityScore = Math.Round(item.Popularity, 3),
                Reasons = outcome.IsPersonalized
                    ? item.Reasons
                    : item.Reasons.Prepend("Još nemamo dovoljno podataka o vašem ukusu, pa prikazujemo najpopularnije knjige.").ToList()
            }).ToList()
        };
    }

    private async Task<List<UserSignal>> LoadUserSignalsAsync(int userId, List<BookInfo> books)
    {
        var signals = new List<UserSignal>();

        var favorites = await _db.Favorites.AsNoTracking()
            .Where(favorite => favorite.UserId == userId)
            .Select(favorite => favorite.BookId)
            .ToListAsync();
        signals.AddRange(favorites.Select(bookId => new UserSignal(bookId, SignalKind.Favorite)));

        var progress = await _db.ReadingProgresses.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.BookId, p.IsFinished })
            .ToListAsync();
        signals.AddRange(progress.Select(p => new UserSignal(p.BookId, p.IsFinished ? SignalKind.Finished : SignalKind.Started)));

        var reviews = await _db.Reviews.AsNoTracking()
            .Where(review => review.UserId == userId)
            .Select(review => new { review.BookId, review.Rating })
            .ToListAsync();
        signals.AddRange(reviews.Select(review => new UserSignal(review.BookId, review.Rating switch
        {
            >= HighRatingThreshold => SignalKind.HighRating,
            3 => SignalKind.NeutralRating,
            _ => SignalKind.LowRating
        })));

        // Pretraga je slab signal: knjige ciji naslov ili autor odgovara nedavnim upitima.
        var recentQueries = await _db.SearchHistories.AsNoTracking()
            .Where(history => history.UserId == userId)
            .OrderByDescending(history => history.CreatedAt)
            .Select(history => history.Query)
            .Take(RecentSearchesConsidered)
            .ToListAsync();

        var searchedBookIds = books
            .Where(book => recentQueries.Any(query =>
                book.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                || book.AuthorName.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .Select(book => book.Id);

        signals.AddRange(searchedBookIds.Select(bookId => new UserSignal(bookId, SignalKind.Search)));

        return signals;
    }

    // Knjige koje su drugi korisnici visoko ocijenili, dodali u favorite ili procitali do kraja.
    private async Task<Dictionary<int, HashSet<int>>> LoadOtherUsersPositiveBooksAsync(int userId)
    {
        var pairs = new List<(int UserId, int BookId)>();

        var highReviews = await _db.Reviews.AsNoTracking()
            .Where(review => review.UserId != userId && review.Rating >= HighRatingThreshold)
            .Select(review => new { review.UserId, review.BookId })
            .ToListAsync();
        pairs.AddRange(highReviews.Select(row => (row.UserId, row.BookId)));

        var favorites = await _db.Favorites.AsNoTracking()
            .Where(favorite => favorite.UserId != userId)
            .Select(favorite => new { favorite.UserId, favorite.BookId })
            .ToListAsync();
        pairs.AddRange(favorites.Select(row => (row.UserId, row.BookId)));

        var finished = await _db.ReadingProgresses.AsNoTracking()
            .Where(progress => progress.UserId != userId && progress.IsFinished)
            .Select(progress => new { progress.UserId, progress.BookId })
            .ToListAsync();
        pairs.AddRange(finished.Select(row => (row.UserId, row.BookId)));

        return pairs
            .GroupBy(pair => pair.UserId)
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.BookId).ToHashSet());
    }
}
