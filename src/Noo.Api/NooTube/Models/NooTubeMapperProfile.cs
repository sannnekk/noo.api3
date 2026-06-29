using AutoMapper;
using Noo.Api.Core.Utils.AutoMapper;
using Noo.Api.NooTube.DTO;
using Noo.Api.NooTube.Engines;

namespace Noo.Api.NooTube.Models;

[AutoMapperProfile]
public class NooTubeMapperProfile : Profile
{
    public NooTubeMapperProfile()
    {
        // NooTubeVideo
        CreateMap<NooTubeVideoModel, NooTubeVideoDTO>()
            .ForMember(dest => dest.IsFavourite, opt => opt.MapFrom(src => src.Favourites.Any()));

        CreateMap<NooTubeVideoModel, UpdateNooTubeVideoDTO>();

        CreateMap<UpdateNooTubeVideoDTO, NooTubeVideoModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UploadedById, opt => opt.Ignore())
            .ForMember(dest => dest.UploadedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Thumbnail, opt => opt.Ignore())
            .ForMember(dest => dest.Reactions, opt => opt.Ignore())
            .ForMember(dest => dest.Favourites, opt => opt.Ignore())
            .ForMember(dest => dest.Comments, opt => opt.Ignore())
            .ForMember(dest => dest.CourseMaterialContents, opt => opt.Ignore());

        // NooTubeVideoStatistics
        CreateMap<VideoStatistics, NooTubeVideoStatisticsDTO>();
        CreateMap<VideoStatisticsPoint, NooTubeVideoStatisticsPointDTO>();

        // NooTubeVideoComment
        CreateMap<NooTubeVideoCommentModel, NooTubeVideoCommentDTO>();

        CreateMap<CreateNooTubeVideoCommentDTO, NooTubeVideoCommentModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.VideoId, opt => opt.Ignore())
            .ForMember(dest => dest.Video, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());

        CreateMap<NooTubeVideoCommentModel, UpdateNooTubeVideoCommentDTO>();

        CreateMap<UpdateNooTubeVideoCommentDTO, NooTubeVideoCommentModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.VideoId, opt => opt.Ignore())
            .ForMember(dest => dest.Video, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());
    }
}
