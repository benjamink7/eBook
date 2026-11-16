using eBook.Model.Common;

namespace eBook.Model.Genres;

public class GenreSearchRequest : PagedRequest
{
    public string? Search { get; set; }
}
