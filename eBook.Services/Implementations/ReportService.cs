using eBook.Model.Enums;
using eBook.Model.Exceptions;
using eBook.Model.Reports;
using eBook.Services.Database;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<RevenueReportResponse> GetRevenueAsync(ReportRequest request)
    {
        var (from, toExclusive) = ResolvePeriod(request);
        var paidStatuses = new[] { PaymentStatus.Completed, PaymentStatus.Refunded };

        var rows = await _db.Payments
            .AsNoTracking()
            .Where(payment => paidStatuses.Contains(payment.Status)
                && payment.PaidAt != null
                && payment.PaidAt >= from
                && payment.PaidAt < toExclusive)
            .OrderBy(payment => payment.PaidAt)
            .Select(payment => new RevenueReportRow
            {
                PaymentId = payment.Id,
                Username = payment.User.Username,
                PlanName = payment.Subscription.SubscriptionPlan.Name,
                Amount = payment.Amount,
                Currency = payment.Currency.ToUpper(),
                Status = payment.Status,
                PaidAt = payment.PaidAt!.Value,
                RefundedAmount = payment.RefundedAmount ?? 0
            })
            .ToListAsync();

        var totals = rows
            .GroupBy(row => row.Currency)
            .Select(group => new RevenueReportTotal
            {
                Currency = group.Key,
                Count = group.Count(),
                Paid = group.Sum(row => row.Amount),
                Refunded = group.Sum(row => row.RefundedAmount),
                Net = group.Sum(row => row.Amount - row.RefundedAmount)
            })
            .OrderBy(total => total.Currency)
            .ToList();

        return new RevenueReportResponse
        {
            From = from,
            To = toExclusive.AddDays(-1),
            Rows = rows,
            Totals = totals
        };
    }

    public async Task<PopularBooksReportResponse> GetPopularBooksAsync(PopularBooksReportRequest request)
    {
        var (from, toExclusive) = ResolvePeriod(request);

        var query = _db.Books.AsNoTracking().AsQueryable();

        if (request.GenreId.HasValue)
        {
            query = query.Where(book => book.BookGenres.Any(bookGenre => bookGenre.GenreId == request.GenreId.Value));
        }

        var rows = await query
            .Select(book => new PopularBookRow
            {
                BookId = book.Id,
                Title = book.Title,
                AuthorName = book.Author.FirstName + " " + book.Author.LastName,
                AccessType = book.AccessType,
                Readers = book.ReadingProgresses.Count(progress => progress.LastReadAt >= from && progress.LastReadAt < toExclusive),
                FinishedCount = book.ReadingProgresses.Count(progress =>
                    progress.IsFinished && progress.LastReadAt >= from && progress.LastReadAt < toExclusive),
                ReviewCount = book.Reviews.Count(review => review.CreatedAt >= from && review.CreatedAt < toExclusive),
                AverageRating = book.Reviews
                    .Where(review => review.CreatedAt >= from && review.CreatedAt < toExclusive)
                    .Average(review => (double?)review.Rating),
                FavoritesCount = book.Favorites.Count(favorite => favorite.CreatedAt >= from && favorite.CreatedAt < toExclusive)
            })
            .OrderByDescending(row => row.Readers)
            .ThenByDescending(row => row.ReviewCount)
            .ThenBy(row => row.Title)
            .Take(request.Top)
            .ToListAsync();

        return new PopularBooksReportResponse
        {
            From = from,
            To = toExclusive.AddDays(-1),
            Rows = rows
        };
    }

    // Period je ukljucivo od pocetka prvog do kraja zadnjeg dana, a datumi se tumace kao UTC.
    private static (DateTime From, DateTime ToExclusive) ResolvePeriod(ReportRequest request)
    {
        var from = DateTime.SpecifyKind(request.From!.Value.Date, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(request.To!.Value.Date, DateTimeKind.Utc);

        if (from > to)
        {
            throw new BusinessException("Datum početka ne smije biti poslije datuma završetka.");
        }

        return (from, to.AddDays(1));
    }
}
