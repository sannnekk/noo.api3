using AutoMapper;
using Noo.Api.Core.Utils.AutoMapper;
using Noo.Api.NooTube.DTO;

namespace Noo.Api.NooTube.Models;

[AutoMapperProfile]
public class NooTubeMapperProfile : Profile
{
    public NooTubeMapperProfile()
    {
        // NooTubeVideo
        CreateMap<NooTubeVideoModel, NooTubeVideoDTO>()
            // TODO: add mapping
            .ForMember(dest => dest.Comments, opt => opt.Ignore());

        CreateMap<NooTubeVideoDTO, NooTubeVideoModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UploadedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Reactions, opt => opt.Ignore())
            .ForMember(dest => dest.Comments, opt => opt.Ignore())
            .ForMember(dest => dest.CourseMaterialContents, opt => opt.Ignore());
    }
}
