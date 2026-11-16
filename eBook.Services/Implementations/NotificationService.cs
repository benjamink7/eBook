using eBook.Model.Common;
using eBook.Model.Exceptions;
using eBook.Model.Notifications;
using eBook.Services.Database;
using eBook.Services.Extensions;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NotificationService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<PagedResult<NotificationResponse>> GetAsync(NotificationSearchRequest request)
    {
        var userId = _currentUser.UserId;
        var query = _db.Notifications.AsNoTracking().Where(notification => notification.UserId == userId);

        if (request.IsRead.HasValue)
        {
            query = query.Where(notification => notification.IsRead == request.IsRead.Value);
        }

        return query
            .OrderByDescending(notification => notification.CreatedAt)
            .Select(notification => new NotificationResponse
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            })
            .ToPagedResultAsync(request);
    }

    public async Task<UnreadCountResponse> GetUnreadCountAsync()
    {
        var userId = _currentUser.UserId;

        var count = await _db.Notifications.CountAsync(notification => notification.UserId == userId && !notification.IsRead);

        return new UnreadCountResponse(count);
    }

    public async Task MarkAsReadAsync(int id)
    {
        var userId = _currentUser.UserId;

        var updated = await _db.Notifications
            .Where(notification => notification.Id == id && notification.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(notification => notification.IsRead, true));

        if (updated == 0)
        {
            throw new NotFoundException("Notifikacija nije pronađena.");
        }
    }

    public async Task MarkAllAsReadAsync()
    {
        var userId = _currentUser.UserId;

        await _db.Notifications
            .Where(notification => notification.UserId == userId && !notification.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(notification => notification.IsRead, true));
    }
}
