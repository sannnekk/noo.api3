using AutoMapper;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.AutoMapper;
using Noo.Api.Support.DTO;

namespace Noo.Api.Support.Models;

[AutoMapperProfile]
public class SupportMapperProfile : Profile
{
    public SupportMapperProfile()
    {
        CreateMap<CreateSupportArticleDTO, SupportArticleModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => Slug.Generate(src.Title)))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<SupportArticleModel, UpdateSupportArticleDTO>();

        CreateMap<UpdateSupportArticleDTO, SupportArticleModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Slug, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore());

        CreateMap<SupportArticleModel, SupportArticleDTO>();
    }
}
