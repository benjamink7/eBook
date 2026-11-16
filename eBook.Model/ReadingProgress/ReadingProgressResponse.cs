namespace eBook.Model.ReadingProgress;

public class ReadingProgressResponse
{
    public int BookId { get; set; }
    public string Title { get; set; } = null!;
    public bool HasCover { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool IsFinished { get; set; }
    public DateTime LastReadAt { get; set; }
    public string? CoverUrl => HasCover ? $"/api/Books/{BookId}/cover" : null;
}
