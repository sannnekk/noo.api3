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

        CreateMap<CreateSupportFaqItemDTO, SupportFaqItemModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<SupportFaqItemModel, UpdateSupportFaqItemDTO>();

        CreateMap<UpdateSupportFaqItemDTO, SupportFaqItemModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            // The DTO's nullable members double as "not part of this patch", so
            // only the ones that came back with a value are written. Category is
            // the exception and is mapped unconditionally: null there means the
            // item belongs to no category, which is a value in its own right.
            .ForMember(dest => dest.Question, opt => opt.Condition(src => src.Question != null))
            .ForMember(dest => dest.Answer, opt => opt.Condition(src => src.Answer != null))
            .ForMember(dest => dest.Order, opt => opt.Condition(src => src.Order.HasValue))
            .ForMember(dest => dest.IsActive, opt => opt.Condition(src => src.IsActive.HasValue));

        CreateMap<SupportFaqItemModel, SupportFaqItemDTO>();
    }
}
