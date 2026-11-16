using eBook.Model.Common;

namespace eBook.Model.Authors;

public class AuthorSearchRequest : PagedRequest
{
    public string? Search { get; set; }
}
