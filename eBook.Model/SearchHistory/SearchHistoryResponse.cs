namespace eBook.Model.SearchHistory;

public class SearchHistoryResponse
{
    public string Query { get; set; } = null!;
    public DateTime LastSearchedAt { get; set; }
}
