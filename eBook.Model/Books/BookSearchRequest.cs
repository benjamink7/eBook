using eBook.Model.Common;
using eBook.Model.Enums;

namespace eBook.Model.Books;

public class BookSearchRequest : PagedRequest
{
    public string? Search { get; set; }
    public int? GenreId { get; set; }
    public int? AuthorId { get; set; }
    public BookAccessType? AccessType { get; set; }
    public BookSortBy SortBy { get; set; } = BookSortBy.Title;
}
