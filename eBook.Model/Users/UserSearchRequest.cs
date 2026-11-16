using eBook.Model.Common;

namespace eBook.Model.Users;

public class UserSearchRequest : PagedRequest
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}
