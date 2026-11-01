using eBook.Model.Common;

namespace eBook.Model.Cities;

public class CitySearchRequest : PagedRequest
{
    public string? Search { get; set; }
    public int? CountryId { get; set; }
}
