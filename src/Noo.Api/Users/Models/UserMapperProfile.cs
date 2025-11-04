using AutoMapper;
using Noo.Api.Core.Utils.AutoMapper;
using Noo.Api.Users.DTO;
using Noo.Api.Users.Types;

namespace Noo.Api.Users.Models;

[AutoMapperProfile]
public class UserMapperProfile : Profile
{
    public UserMapperProfile()
    {
        // user
        CreateMap<UserModel, UserDTO>();
        CreateMap<UserCreationPayload, UserModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.TelegramId, opt => opt.Ignore())
            .ForMember(dest => dest.TelegramUsername, opt => opt.Ignore())
            .ForMember(dest => dest.CoursesAsMember, opt => opt.Ignore())
            .ForMember(dest => dest.CoursesAsAssigner, opt => opt.Ignore())
            .ForMember(dest => dest.CoursesAsAuthor, opt => opt.Ignore())
            .ForMember(dest => dest.CoursesAsEditor, opt => opt.Ignore())
            .ForMember(dest => dest.CourseMaterialReactions, opt => opt.Ignore())
            .ForMember(dest => dest.Avatar, opt => opt.Ignore())
            .ForMember(dest => dest.Sessions, opt => opt.Ignore())
            .ForMember(dest => dest.Snippets, opt => opt.Ignore())
            .ForMember(dest => dest.PollParticipations, opt => opt.Ignore())
            .ForMember(dest => dest.CalendarEvents, opt => opt.Ignore())
            .ForMember(dest => dest.Notifications, opt => opt.Ignore())
            .ForMember(dest => dest.Settings, opt => opt.Ignore())
            .ForMember(dest => dest.UploadedVideos, opt => opt.Ignore())
            .ForMember(dest => dest.NooTubeVideoComments, opt => opt.Ignore())
            .ForMember(dest => dest.NooTubeVideoReactions, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedWorkHistoryChanges, opt => opt.Ignore())
            .ForMember(dest => dest.MentorAssignmentsAsMentor, opt => opt.Ignore())
            .ForMember(dest => dest.MentorAssignmentsAsStudent, opt => opt.Ignore())
            .ForMember(dest => dest.IsBlocked, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(_ => false));


        // mentor assignment
        CreateMap<MentorAssignmentModel, MentorAssignmentDTO>()
            .ForMember(d => d.Student, opt => opt.Ignore())
            .ForMember(d => d.Mentor, opt => opt.Ignore())
            .ForMember(d => d.Subject, opt => opt.Ignore());
    }
}
