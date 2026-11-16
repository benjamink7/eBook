using eBook.Model.Constants;
using eBook.Model.Enums;
using eBook.Model.Statistics;
using eBook.Services.Database;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class StatisticsService : IStatisticsService
{
    private const int MonthsShown = 6;
    private const int TopBooksShown = 5;

    private readonly ApplicationDbContext _db;

    public StatisticsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<StatisticsResponse> GetAsync()
    {
        var now = DateTime.UtcNow;
        var currentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var firstMonth = currentMonth.AddMonths(-(MonthsShown - 1));

        var paidStatuses = new[] { PaymentStatus.Completed, PaymentStatus.Refunded };

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(payment => paidStatuses.Contains(payment.Status) && payment.PaidAt != null)
            .Select(payment => new PaymentSnapshot(
                payment.PaidAt!.Value, payment.Currency, payment.Amount - (payment.RefundedAmount ?? 0)))
            .ToListAsync();

        var subscriptionDates = await _db.Subscriptions
            .AsNoTracking()
            .Where(subscription => subscription.CreatedAt >= firstMonth)
            .Select(subscription => subscription.CreatedAt)
            .ToListAsync();

        var months = Enumerable.Range(0, MonthsShown)
            .Select(offset => firstMonth.AddMonths(offset))
            .Select(month => new MonthlyStatistic
            {
                Month = month.ToString("yyyy-MM"),
                NewSubscriptions = subscriptionDates.Count(date => date.Year == month.Year && date.Month == month.Month),
                Revenue = SumByCurrency(payments.Where(p => p.PaidAt.Year == month.Year && p.PaidAt.Month == month.Month))
            })
            .ToList();

        var topBooks = await _db.Books
            .AsNoTracking()
            .OrderByDescending(book => book.ReadingProgresses.Count)
            .ThenBy(book => book.Title)
            .Take(TopBooksShown)
            .Select(book => new TopBookStatistic
            {
                BookId = book.Id,
                Title = book.Title,
                AuthorName = book.Author.FirstName + " " + book.Author.LastName,
                Readers = book.ReadingProgresses.Count,
                Favorites = book.Favorites.Count,
                AverageRating = book.Reviews.Average(review => (double?)review.Rating)
            })
            .ToListAsync();

        return new StatisticsResponse
        {
            TotalUsers = await _db.Users.CountAsync(user => user.UserRoles.Any(role => role.Role.Name == AppRoles.User)),
            ActiveSubscriptions = await _db.Subscriptions.CountAsync(subscription => subscription.Status == SubscriptionStatus.Active),
            TotalBooks = await _db.Books.CountAsync(book => book.IsActive),
            PremiumBooks = await _db.Books.CountAsync(book => book.IsActive && book.AccessType == BookAccessType.Premium),
            TotalRevenue = SumByCurrency(payments),
            RevenueThisMonth = SumByCurrency(payments.Where(p => p.PaidAt >= currentMonth)),
            Months = months,
            TopBooks = topBooks
        };
    }

    private static List<CurrencyAmount> SumByCurrency(IEnumerable<PaymentSnapshot> payments) =>
        payments
            .GroupBy(payment => payment.Currency.ToUpperInvariant())
            .Select(group => new CurrencyAmount { Currency = group.Key, Amount = group.Sum(p => p.Net) })
            .OrderBy(amount => amount.Currency)
            .ToList();

    private sealed record PaymentSnapshot(DateTime PaidAt, string Currency, decimal Net);
}
