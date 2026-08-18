using AutoMapper;
using Noo.Api.Auth.External.DTO;
using Noo.Api.Auth.External.Models;
using Noo.Api.Core.Utils.AutoMapper;

namespace Noo.Api.Auth.External;

[AutoMapperProfile]
public class ExternalAuthMapperProfile : Profile
{
    public ExternalAuthMapperProfile()
    {
        CreateMap<UserIdentityModel, LinkedIdentityDTO>()
            .ForMember(dest => dest.LinkedAt, opt => opt.MapFrom(src => src.CreatedAt));
    }
}
