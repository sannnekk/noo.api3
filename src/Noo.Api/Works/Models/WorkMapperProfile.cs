using AutoMapper;
using Noo.Api.Core.Utils.AutoMapper;
using Noo.Api.Works.DTO;

namespace Noo.Api.Works.Models;

[AutoMapperProfile]
public class WorkMapperProfile : Profile
{
    public WorkMapperProfile()
    {
        // work task
        CreateMap<WorkTaskModel, WorkTaskDTO>();

        CreateMap<CreateWorkTaskDTO, WorkTaskModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.WorkId, opt => opt.Ignore())
            .ForMember(dest => dest.Work, opt => opt.Ignore());

        CreateMap<WorkTaskModel, UpdateWorkTaskDTO>();

        CreateMap<UpdateWorkTaskDTO, WorkTaskModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.WorkId, opt => opt.Ignore())
            .ForMember(dest => dest.Work, opt => opt.Ignore());

        // work
        CreateMap<WorkModel, UpdateWorkDTO>()
            .ForMember(dest => dest.Tasks, opt => opt.MapFrom((src, _, _, context) =>
                src.Tasks.MapCollectionToDictionary<WorkTaskModel, UpdateWorkTaskDTO>(context)));

        CreateMap<WorkModel, WorkDTO>();

        // Tasks are handled in AfterMap so AutoMapper's default collection mapper
        // (which calls dest.Tasks.Clear() before re-adding) never touches the EF-tracked
        // collection — that Clear would orphan existing rows under cascade FKs and
        // delete them despite the merge re-adding them seconds later.
        CreateMap<UpdateWorkDTO, WorkModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Subject, opt => opt.Ignore())
            .ForMember(dest => dest.CourseWorkAssignments, opt => opt.Ignore())
            .ForMember(dest => dest.Tasks, opt => opt.Ignore())
            .AfterMap((src, dest, context) =>
            {
                dest.Tasks = src.Tasks.MapDictionaryToCollection<UpdateWorkTaskDTO, WorkTaskModel>(
                    dest.Tasks, context.Mapper);
            });

        CreateMap<CreateWorkDTO, WorkModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Subject, opt => opt.Ignore())
            .ForMember(dest => dest.CourseWorkAssignments, opt => opt.Ignore());
    }
}
