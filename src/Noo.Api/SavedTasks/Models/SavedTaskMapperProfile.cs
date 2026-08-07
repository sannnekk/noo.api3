using AutoMapper;
using Noo.Api.Core.Utils.AutoMapper;
using Noo.Api.SavedTasks.DTO;

namespace Noo.Api.SavedTasks.Models;

[AutoMapperProfile]
public class SavedTaskMapperProfile : Profile
{
    public SavedTaskMapperProfile()
    {
        // The work is not a column of the saved task: it is read through the
        // task, which is where it belongs. Its subject comes along inside it.
        CreateMap<SavedTaskModel, SavedTaskDTO>()
            .ForMember(dest => dest.WorkId, opt => opt.MapFrom(src => src.Task.WorkId))
            .ForMember(dest => dest.Work, opt => opt.MapFrom(src => src.Task.Work));
    }
}
