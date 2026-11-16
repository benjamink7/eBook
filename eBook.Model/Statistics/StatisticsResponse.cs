namespace eBook.Model.Statistics;

public class CurrencyAmount
{
    public string Currency { get; set; } = null!;
    public decimal Amount { get; set; }
}

public class MonthlyStatistic
{
    public string Month { get; set; } = null!;
    public int NewSubscriptions { get; set; }
    public List<CurrencyAmount> Revenue { get; set; } = new();
}

public class TopBookStatistic
{
    public int BookId { get; set; }
    public string Title { get; set; } = null!;
    public string AuthorName { get; set; } = null!;
    public int Readers { get; set; }
    public int Favorites { get; set; }
    public double? AverageRating { get; set; }
}

public class StatisticsResponse
{
    public int TotalUsers { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int TotalBooks { get; set; }
    public int PremiumBooks { get; set; }
    public List<CurrencyAmount> TotalRevenue { get; set; } = new();
    public List<CurrencyAmount> RevenueThisMonth { get; set; } = new();
    public List<MonthlyStatistic> Months { get; set; } = new();
    public List<TopBookStatistic> TopBooks { get; set; } = new();
}
