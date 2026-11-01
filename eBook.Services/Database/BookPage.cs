namespace eBook.Services.Database;

public class BookPage
{
    public int Id { get; set; }
    public int PageNumber { get; set; }
    public string Content { get; set; } = null!;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
}
