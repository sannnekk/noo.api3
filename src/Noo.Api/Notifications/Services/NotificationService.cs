using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Filters;
using Noo.Api.Notifications.Models;
using Noo.Api.Notifications.Types;

namespace Noo.Api.Notifications.Services;

[RegisterScoped(typeof(INotificationService))]
public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationRepository _repository;
    private readonly IEventPublisher _events;

    public NotificationService(IUnitOfWork unitOfWork, INotificationRepository notificationRepository, IEventPublisher events)
    {
        _unitOfWork = unitOfWork;
        _repository = notificationRepository;
        _events = events;
    }

    public async Task BulkCreateNotificationsAsync(BulkCreateNotificationsDTO options)
    {
        foreach (var userId in options.UserIds)
        {
            var model = new NotificationModel
            {
                UserId = userId,
                Type = options.Type,
                Title = options.Title,
                Message = options.Message,
                IsBanner = options.IsBanner,
                IsRead = false,
                Link = options.Link,
                LinkText = options.LinkText
            };

            _repository.Add(model);

            // Publish event for delivery handlers
            await _events.PublishAsync(new NotificationCreatedEvent(model, options.Channels));
        }

        await _unitOfWork.CommitAsync();
    }

    public async Task CreateNotificationAsync(CreateNotificationDTO options)
    {
        var model = new NotificationModel
        {
            UserId = options.UserId,
            Type = options.Type,
            Title = options.Title,
            Message = options.Message,
            IsBanner = options.IsBanner,
            IsRead = false,
            Link = options.Link,
            LinkText = options.LinkText
        };

        _repository.Add(model);

        await _unitOfWork.CommitAsync();
        await _events.PublishAsync(new NotificationCreatedEvent(model));
    }

    public async Task DeleteNotificationAsync(Ulid notificationId, Ulid userId)
    {
        await _repository.DeleteForUserAsync(userId, notificationId);
        await _unitOfWork.CommitAsync();
    }

    public Task<SearchResult<NotificationModel>> GetNotificationsAsync(Ulid userId, NotificationFilter filter)
    {
        return _repository.GetForUserAsync(userId, filter);
    }

    public async Task MarkAsReadAsync(Ulid userId, Ulid notificationId)
    {
        await _repository.MarkAsReadAsync(userId, notificationId);
        await _unitOfWork.CommitAsync();
    }
}

public record NotificationCreatedEvent(NotificationModel Model, IEnumerable<NotificationChannelType>? Channels = null) : IDomainEvent;
