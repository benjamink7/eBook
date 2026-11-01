using eBook.Model.Enums;

namespace eBook.Services.Database;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int PublishedYear { get; set; }
    public int PageCount { get; set; }
    public BookAccessType AccessType { get; set; }
    public byte[]? CoverImage { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;

    public ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
    public ICollection<BookPage> Pages { get; set; } = new List<BookPage>();
    public ICollection<ReadingProgress> ReadingProgresses { get; set; } = new List<ReadingProgress>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}
