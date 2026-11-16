using eBook.Model.Common;

namespace eBook.Model.Countries;

public class CountrySearchRequest : PagedRequest
{
    public string? Search { get; set; }
}
