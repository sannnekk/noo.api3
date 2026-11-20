using AutoMapper;
using Noo.Api.Core.Utils.AutoMapper;
using Noo.Api.Notifications.DTO;

namespace Noo.Api.Notifications.Models;

[AutoMapperProfile]
public class NotificationMapperProfile : Profile
{
    public NotificationMapperProfile()
    {
        CreateMap<NotificationModel, NotificationDTO>();
        CreateMap<PushSubscriptionModel, PushSubscriptionDTO>();
    }
}
