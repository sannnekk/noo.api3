using AutoMapper;
using Noo.Api.Core.Utils.AutoMapper;
using Noo.Api.Platform.DTO;

namespace Noo.Api.Platform.Models;

[AutoMapperProfile]
public class PlatformMapperProfile : Profile
{
    public PlatformMapperProfile()
    {
        CreateMap<PlatformSettingsModel, PlatformSettingsDTO>();

        CreateMap<PlatformSettingsModel, UpdatePlatformSettingsDTO>();

        // A member that came back null was not part of the patch, so only the
        // ones carrying a value are written. Clearing a link is not offered —
        // an empty footer link is a broken page, not a setting anyone wants.
        CreateMap<UpdatePlatformSettingsDTO, PlatformSettingsModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ShopLink, opt => opt.Condition(src => src.ShopLink != null))
            .ForMember(
                dest => dest.PrivacyPolicyLink,
                opt => opt.Condition(src => src.PrivacyPolicyLink != null)
            )
            .ForMember(dest => dest.TermsLink, opt => opt.Condition(src => src.TermsLink != null))
            .ForMember(
                dest => dest.SupportChatLink,
                opt => opt.Condition(src => src.SupportChatLink != null)
            )
            .ForMember(
                dest => dest.SupportChatName,
                opt => opt.Condition(src => src.SupportChatName != null)
            )
            .ForMember(
                dest => dest.SupportEmail,
                opt => opt.Condition(src => src.SupportEmail != null)
            )
            .ForMember(
                dest => dest.SupportResponseTime,
                opt => opt.Condition(src => src.SupportResponseTime != null)
            );
    }
}
