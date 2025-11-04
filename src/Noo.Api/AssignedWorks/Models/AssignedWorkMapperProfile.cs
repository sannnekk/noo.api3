using AutoMapper;
using Noo.Api.AssignedWorks.DTO;
using Noo.Api.Core.Utils.AutoMapper;

namespace Noo.Api.AssignedWorks.Models;

[AutoMapperProfile]
public class AssignedWorkMapperProfile : Profile
{
    public AssignedWorkMapperProfile()
    {
        CreateMap<UpsertAssignedWorkAnswerDTO, AssignedWorkAnswerModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id ?? Ulid.NewUlid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedWorkId, opt => opt.Ignore())
            .ForMember(dest => dest.Task, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpsertAssignedWorkCommentDTO, AssignedWorkCommentModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Ulid.NewUlid()))
            .ForMember(dest => dest.AssignedWorkAsHelperMentor, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedWorkAsMainMentor, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedWorkAsStudent, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<AssignedWorkModel, AssignedWorkDTO>();
    }
}
