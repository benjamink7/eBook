namespace eBook.Services.Database;

public class Genre
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
}
