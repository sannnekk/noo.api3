using AutoMapper;
using Noo.Api.Core.Utils.AutoMapper;
using Noo.Api.Polls.DTO;

namespace Noo.Api.Polls.Models;

[AutoMapperProfile]
public class PollMapperProfile : Profile
{
    public PollMapperProfile()
    {
        // Poll
        CreateMap<PollModel, PollDTO>();

        CreateMap<CreatePollDTO, PollModel>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.Participations, o => o.Ignore())
            .ForMember(d => d.Questions, o => o.MapFrom(s => s.Questions))
            .ForMember(d => d.CourseMaterialContents, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive ?? true))
            .ForMember(d => d.IsAuthRequired, o => o.MapFrom(s => s.IsAuthRequired ?? true));

        CreateMap<PollModel, UpdatePollDTO>();

        CreateMap<UpdatePollDTO, PollModel>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.Participations, o => o.Ignore())
            .ForMember(d => d.CourseMaterialContents, o => o.Ignore())
            .ForMember(d => d.Questions, o => o.Ignore());

        // Question
        CreateMap<PollQuestionModel, PollQuestionDTO>();

        CreateMap<CreatePollQuestionDTO, PollQuestionModel>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.PollId, o => o.Ignore())
            .ForMember(d => d.Poll, o => o.Ignore())
            .ForMember(d => d.Answers, o => o.Ignore())
            .ForMember(d => d.Order, o => o.MapFrom(_ => 0))
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type))
            .ForMember(d => d.Config, o => o.MapFrom(s => s.Config));

        // Answer
        CreateMap<PollAnswerModel, PollAnswerDTO>();
        CreateMap<PollAnswerModel, UpdatePollAnswerDTO>();

        CreateMap<UpdatePollAnswerDTO, PollAnswerModel>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.PollQuestionId, o => o.Ignore())
            .ForMember(d => d.PollQuestion, o => o.Ignore());

        // Participation
        CreateMap<DTO.PollParticipationDTO, PollParticipationModel>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.PollId, o => o.Ignore())
            .ForMember(d => d.Poll, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.Answers, o => o.Ignore())
            .ForMember(d => d.UserExternalData, o => o.Ignore());

        CreateMap<CreatePollParticipationDTO, PollParticipationModel>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.PollId, o => o.Ignore())
            .ForMember(d => d.Poll, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.Answers, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.UserExternalData, o => o.Ignore());
    }
}
