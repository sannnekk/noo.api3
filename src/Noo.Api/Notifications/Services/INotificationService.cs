using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Filters;
using Noo.Api.Notifications.Models;

namespace Noo.Api.Notifications.Services;

public interface INotificationService
{
    public Task CreateNotificationAsync(CreateNotificationDTO options);
    public Task BulkCreateNotificationsAsync(BulkCreateNotificationsDTO options);
    public Task DeleteNotificationAsync(Ulid notificationId, Ulid userId);
    public Task<SearchResult<NotificationModel>> GetNotificationsAsync(Ulid userId, NotificationFilter filter);
    public Task MarkAsReadAsync(Ulid userId, Ulid notificationId);
    public Task MarkAllAsReadAsync(Ulid userId);
}
