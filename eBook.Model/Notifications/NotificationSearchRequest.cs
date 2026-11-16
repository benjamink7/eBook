using eBook.Model.Common;

namespace eBook.Model.Notifications;

public class NotificationSearchRequest : PagedRequest
{
    public bool? IsRead { get; set; }
}
