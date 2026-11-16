using eBook.Model.Enums;

namespace eBook.Model.Reports;

public class PopularBookRow
{
    public int BookId { get; set; }
    public string Title { get; set; } = null!;
    public string AuthorName { get; set; } = null!;
    public BookAccessType AccessType { get; set; }
    public int Readers { get; set; }
    public int FinishedCount { get; set; }
    public int ReviewCount { get; set; }
    public double? AverageRating { get; set; }
    public int FavoritesCount { get; set; }
}

public class PopularBooksReportResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<PopularBookRow> Rows { get; set; } = new();
}
