using eBook.Model.Common;
using eBook.Model.Notifications;

namespace eBook.Services.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationResponse>> GetAsync(NotificationSearchRequest request);
    Task<UnreadCountResponse> GetUnreadCountAsync();
    Task MarkAsReadAsync(int id);
    Task MarkAllAsReadAsync();
}
