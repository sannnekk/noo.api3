using AutoMapper;
using Noo.Api.Core.Utils.AutoMapper;
using Noo.Api.UserHistory.DTO;

namespace Noo.Api.UserHistory.Models;

[AutoMapperProfile]
public class UserHistoryMapperProfile : Profile
{
    public UserHistoryMapperProfile()
    {
        CreateMap<UserHistoryModel, UserHistoryDTO>();
    }
}
