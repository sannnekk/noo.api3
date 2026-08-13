using AutoMapper;
using Noo.Api.Core.Utils.AutoMapper;
using Noo.Api.GoogleSheetsIntegrations.DTO;
using Noo.Api.GoogleSheetsIntegrations.Exports;

namespace Noo.Api.GoogleSheetsIntegrations.Models;

[AutoMapperProfile]
public class GoogleSheetsIntegrationMapperProfile : Profile
{
    public GoogleSheetsIntegrationMapperProfile()
    {
        CreateMap<ExportParameters, ExportParametersDTO>().ReverseMap();

        CreateMap<GoogleSheetsIntegrationModel, GoogleSheetsIntegrationDTO>()
            .ForMember(
                dest => dest.GoogleAccount,
                opt => opt.MapFrom(src => src.GoogleAuthData.AccountEmail)
            );

    }
}
