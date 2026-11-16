using eBook.Model.Common;

namespace eBook.Model.News;

public class NewsSearchRequest : PagedRequest
{
    public string? Search { get; set; }
}
