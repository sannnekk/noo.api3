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
            .ForMember(d => d.ParticipationsCount, o => o.Ignore())
            .ForMember(d => d.HasParticipated, o => o.Ignore())
            .ForMember(d => d.Questions, o => o.MapFrom(s => s.Questions))
            .ForMember(d => d.CourseMaterialContents, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive ?? true))
            .ForMember(d => d.IsAuthRequired, o => o.MapFrom(s => s.IsAuthRequired ?? true));

        CreateMap<PollModel, UpdatePollDTO>()
            .ForMember(
                d => d.Questions,
                o =>
                    o.MapFrom(
                        (src, _, _, context) =>
                            src.Questions.MapCollectionToDictionary<
                                PollQuestionModel,
                                UpdatePollQuestionDTO
                            >(context)
                    )
            );

        // Questions are merged in AfterMap so AutoMapper's default collection mapper
        // (which calls dest.Questions.Clear() before re-adding) never touches the
        // EF-tracked collection — that clear would orphan the existing questions and
        // cascade-delete them together with their answers, despite the merge re-adding
        // them moments later.
        CreateMap<UpdatePollDTO, PollModel>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.Participations, o => o.Ignore())
            .ForMember(d => d.ParticipationsCount, o => o.Ignore())
            .ForMember(d => d.HasParticipated, o => o.Ignore())
            .ForMember(d => d.CourseMaterialContents, o => o.Ignore())
            .ForMember(d => d.Questions, o => o.Ignore())
            .AfterMap(
                (src, dest, context) =>
                {
                    dest.Questions = src.Questions.MapDictionaryToCollection<
                        UpdatePollQuestionDTO,
                        PollQuestionModel
                    >(dest.Questions, context.Mapper);
                }
            );

        // Question
        CreateMap<PollQuestionModel, PollQuestionDTO>();

        CreateMap<PollQuestionModel, UpdatePollQuestionDTO>();

        CreateMap<UpdatePollQuestionDTO, PollQuestionModel>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.PollId, o => o.Ignore())
            .ForMember(d => d.Poll, o => o.Ignore())
            .ForMember(d => d.Answers, o => o.Ignore());

        CreateMap<CreatePollQuestionDTO, PollQuestionModel>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.PollId, o => o.Ignore())
            .ForMember(d => d.Poll, o => o.Ignore())
            .ForMember(d => d.Answers, o => o.Ignore())
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type))
            .ForMember(d => d.Config, o => o.MapFrom(s => s.Config));

        // Answer
        CreateMap<PollAnswerModel, PollAnswerDTO>();

        CreateMap<PollAnswerModel, UpdatePollAnswerDTO>()
            .ForMember(
                d => d.MediaIds,
                o =>
                    o.MapFrom(s =>
                        s.Medias == null
                            ? Enumerable.Empty<Ulid>()
                            : s.Medias.Select(media => media.Id)
                    )
            );

        // Like on creation, the files are attached by PollService: it resolves the ids
        // to tracked media rows the DTO knows nothing about.
        CreateMap<UpdatePollAnswerDTO, PollAnswerModel>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.PollQuestionId, o => o.Ignore())
            .ForMember(d => d.PollQuestion, o => o.Ignore())
            .ForMember(d => d.Medias, o => o.Ignore());

        // The files an answer carries are attached by PollService, which resolves
        // the ids to tracked media rows the DTO knows nothing about.
        CreateMap<CreatePollAnswerDTO, PollAnswerModel>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.PollQuestion, o => o.Ignore())
            .ForMember(d => d.Medias, o => o.Ignore())
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Value ?? default));

        // Participation
        CreateMap<PollParticipationModel, DTO.PollParticipationDTO>();

        CreateMap<DTO.PollParticipationDTO, PollParticipationModel>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.PollId, o => o.Ignore())
            .ForMember(d => d.Poll, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.Answers, o => o.Ignore())
            .ForMember(d => d.UserExternalData, o => o.Ignore());

        // The answers are built by PollService rather than mapped here: each one is
        // checked against the question it answers, and a file answer's media have to
        // be resolved to tracked rows before the participation can be stored.
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
