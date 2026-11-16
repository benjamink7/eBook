using eBook.Model.Enums;

namespace eBook.Model.Books;

public class BookListItemResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string AuthorName { get; set; } = null!;
    public int PublishedYear { get; set; }
    public BookAccessType AccessType { get; set; }
    public bool IsActive { get; set; }
    public List<string> Genres { get; set; } = new();
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }

    public bool HasCover { get; set; }
    public string? CoverUrl => HasCover ? $"/api/Books/{Id}/cover" : null;
}
