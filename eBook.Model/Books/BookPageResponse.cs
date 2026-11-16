namespace eBook.Model.Books;

public class BookPageResponse
{
    public int BookId { get; set; }
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public string Content { get; set; } = null!;
}
