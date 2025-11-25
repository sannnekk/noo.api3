using AutoMapper;
using Noo.Api.Core.Utils.AutoMapper;
using Noo.Api.Media.DTO;

namespace Noo.Api.Media.Models;

[AutoMapperProfile]
public class MediaMapperProfile : Profile
{
    public MediaMapperProfile()
    {
        CreateMap<MediaModel, MediaDTO>();

        CreateMap<MediaDTO, MediaModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Courses, opt => opt.Ignore())
            .ForMember(dest => dest.NooTubeVideoThumbnail, opt => opt.Ignore())
            .ForMember(dest => dest.UserAvatar, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CourseMaterialContents, opt => opt.Ignore())
            .ForMember(dest => dest.Hash, opt => opt.Ignore());
    }
}
