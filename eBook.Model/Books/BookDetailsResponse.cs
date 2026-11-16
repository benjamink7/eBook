using eBook.Model.Enums;
using eBook.Model.Genres;

namespace eBook.Model.Books;

public class BookDetailsResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int PublishedYear { get; set; }
    public int PageCount { get; set; }
    public BookAccessType AccessType { get; set; }
    public bool IsActive { get; set; }
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = null!;
    public List<GenreResponse> Genres { get; set; } = new();
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }

    // Moze li trenutni korisnik citati knjigu (besplatna knjiga, Premium pretplata ili admin).
    public bool CanRead { get; set; }

    public bool HasCover { get; set; }
    public string? CoverUrl => HasCover ? $"/api/Books/{Id}/cover" : null;
}
