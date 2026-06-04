using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Notifications.Filters;
using Noo.Api.Notifications.Models;

namespace Noo.Api.Notifications.Services;

public interface INotificationRepository : IRepository<NotificationModel>
{
    public Task<SearchResult<NotificationModel>> GetForUserAsync(Ulid userId, NotificationFilter filter);
    public Task MarkAsReadAsync(Ulid userId, Ulid notificationId);
    public Task MarkAllAsReadAsync(Ulid userId);
    public Task DeleteForUserAsync(Ulid userId, Ulid notificationId);
}

