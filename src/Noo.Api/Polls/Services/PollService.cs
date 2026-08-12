using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Media;
using Noo.Api.Media.Models;
using Noo.Api.Media.Services;
using Noo.Api.Media.Types;
using Noo.Api.Polls.DTO;
using Noo.Api.Polls.Exceptions;
using Noo.Api.Polls.Filters;
using Noo.Api.Polls.Models;
using Noo.Api.Polls.Specifications;
using Noo.Api.Polls.Types;
using SystemTextJsonPatch;

namespace Noo.Api.Polls.Services;

[RegisterScoped(typeof(IPollService))]
public class PollService : IPollService
{
    private readonly IMapper _mapper;
    private readonly IPollRepository _pollRepository;
    private readonly IPollParticipationRepository _pollParticipationRepository;
    private readonly IPollAnswerRepository _pollAnswerRepository;
    private readonly IMediaRepository _mediaRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IJsonPatchUpdateService _jsonPatchUpdateService;

    public PollService(
        IMapper mapper,
        IPollRepository pollRepository,
        IPollParticipationRepository pollParticipationRepository,
        IPollAnswerRepository pollAnswerRepository,
        IMediaRepository mediaRepository,
        ICurrentUser currentUser,
        IJsonPatchUpdateService jsonPatchUpdateService
    )
    {
        _mapper = mapper;
        _currentUser = currentUser;
        _pollRepository = pollRepository;
        _pollParticipationRepository = pollParticipationRepository;
        _pollAnswerRepository = pollAnswerRepository;
        _mediaRepository = mediaRepository;
        _jsonPatchUpdateService = jsonPatchUpdateService;
    }

    public Ulid CreatePoll(CreatePollDTO createPollDto)
    {
        var pollModel = _mapper.Map<PollModel>(createPollDto);
        _pollRepository.Add(pollModel);

        return pollModel.Id;
    }

    public void DeletePoll(Ulid id)
    {
        _pollRepository.DeleteById(id);
    }

    public async Task<PollModel?> GetPollAsync(Ulid id)
    {
        var poll = await _pollRepository.GetWithQuestionsAsync(id);

        poll.ThrowNotFoundIfNull();

        if (!_currentUser.IsAuthenticated && poll.IsAuthRequired)
        {
            return null;
        }

        // Lets the client keep a returning user out of the poll instead of
        // letting them fill it in and be turned away on submit.
        if (_currentUser.UserId.HasValue)
        {
            poll.HasParticipated = await UserAlreadyParticipatedAsync(
                id,
                _currentUser.UserId,
                null
            );
        }

        return poll;
    }

    public Task<PollParticipationModel?> GetPollParticipationAsync(Ulid participationId)
    {
        return _pollParticipationRepository.GetWithAnswersAsync(participationId);
    }

    public Task<SearchResult<PollParticipationModel>> GetPollParticipationsAsync(
        Ulid pollId,
        PollParticipationFilter filter
    )
    {
        filter.PollId = pollId;
        return _pollParticipationRepository.SearchAsync(
            filter,
            [new PollParticipationSearchSpecification(filter.Search)]
        );
    }

    public Task<SearchResult<PollModel>> GetPollsAsync(PollFilter filter)
    {
        return _pollRepository.SearchWithParticipationsCountAsync(filter);
    }

    public Task<SearchResult<PollParticipationModel>> GetUserParticipationsAsync(
        Ulid userId,
        PollParticipationFilter filter
    )
    {
        if (
            userId != _currentUser.UserId
            && !_currentUser.IsInRole(UserRoles.Admin, UserRoles.Teacher)
        )
        {
            throw new ForbiddenException();
        }

        return _pollParticipationRepository.SearchAsync(
            filter,
            [new PollParticipationByUserSpecification(userId, filter.Search)]
        );
    }

    public async Task ParticipateAsync(Ulid pollId, CreatePollParticipationDTO participationDto)
    {
        // Resolve current user id when available
        var currentUserId = _currentUser?.UserId;

        // Only check for duplicates when an identifier is present
        var hasUserId = currentUserId.HasValue;
        var hasExternal = !string.IsNullOrWhiteSpace(participationDto.UserExternalIdentifier);

        if (
            (hasUserId || hasExternal)
            && await UserAlreadyParticipatedAsync(
                pollId,
                currentUserId,
                participationDto.UserExternalIdentifier
            )
        )
        {
            throw new UserAlreadyVotedException();
        }

        var poll = await _pollRepository.GetWithQuestionsAsync(pollId);

        poll.ThrowNotFoundIfNull();

        var participationModel = _mapper.Map<PollParticipationModel>(participationDto);
        participationModel.PollId = pollId;
        // Persist the current user id if present
        if (currentUserId.HasValue)
        {
            participationModel.UserId = currentUserId.Value;
        }
        participationModel.Answers = await BuildAnswersAsync(poll!, participationDto.Answers);

        _pollParticipationRepository.Add(participationModel);
    }

    /// <summary>
    /// Turns the submitted answers into models, checking each one against the question
    /// it claims to answer. The question is the authority on the answer's type, so a
    /// client cannot make an answer read back as something the question never asked for.
    /// </summary>
    private async Task<ICollection<PollAnswerModel>> BuildAnswersAsync(
        PollModel poll,
        IEnumerable<CreatePollAnswerDTO> answerDtos
    )
    {
        var questions = poll.Questions.ToDictionary(question => question.Id);
        var files = await LoadAnswerFilesAsync(answerDtos);
        var answers = new List<PollAnswerModel>();
        var answered = new HashSet<Ulid>();

        foreach (var answerDto in answerDtos)
        {
            if (!questions.TryGetValue(answerDto.PollQuestionId, out var question))
            {
                throw new InvalidPollAnswerException("The question does not belong to this poll.");
            }

            // One answer per question: a repeated one would read as two answers to the
            // same question everywhere the participation is shown.
            if (!answered.Add(answerDto.PollQuestionId))
            {
                throw new InvalidPollAnswerException("The question was answered twice.");
            }

            var answer = _mapper.Map<PollAnswerModel>(answerDto);

            answer.Value = new PollAnswerValue
            {
                Type = question.Type,
                Value = answerDto.Value?.Value,
            };

            if (question.Type == PollQuestionType.Files)
            {
                answer.Medias = ResolveAnswerFiles(question, answerDto.MediaIds, files);
            }
            else if (answerDto.MediaIds.Any())
            {
                throw new InvalidPollAnswerException("This question does not accept files.");
            }

            answers.Add(answer);
        }

        return answers;
    }

    private async Task<IReadOnlyDictionary<Ulid, MediaModel>> LoadAnswerFilesAsync(
        IEnumerable<CreatePollAnswerDTO> answerDtos
    )
    {
        var ids = answerDtos.SelectMany(answer => answer.MediaIds).Distinct().ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Ulid, MediaModel>();
        }

        var media = await _mediaRepository.GetByIdsAsync(ids);

        return media.ToDictionary(item => item.Id);
    }

    /// <summary>
    /// Resolves the files of a single answer, refusing anything the question does not
    /// allow. Uploads are a separate step, so the ids arrive unvouched for: a file only
    /// counts as an answer when the participant uploaded it themselves, for this purpose,
    /// and it fits what the question asks for.
    /// </summary>
    private ICollection<MediaModel> ResolveAnswerFiles(
        PollQuestionModel question,
        IEnumerable<Ulid> mediaIds,
        IReadOnlyDictionary<Ulid, MediaModel> files
    )
    {
        var config = question.Config ?? new PollQuestionConfig();
        var ids = mediaIds.Distinct().ToArray();
        var maxCount = config.MaxFileCount ?? PollFileAnswerLimits.MaxFileCount;

        if (ids.Length > maxCount)
        {
            throw new InvalidPollAnswerException(
                $"This question accepts at most {maxCount} file(s)."
            );
        }

        var resolved = new List<MediaModel>(ids.Length);

        foreach (var id in ids)
        {
            if (!files.TryGetValue(id, out var media))
            {
                throw new InvalidPollAnswerException("Attached file was not found.");
            }

            if (
                media.Category != MediaCategory.PollAnswerFile
                || media.Status != MediaStatus.Completed
                || media.OwnerId != _currentUser.UserId
            )
            {
                throw new InvalidPollAnswerException("Attached file cannot be used as an answer.");
            }

            if (config.MaxFileSize is { } maxFileSize && media.Size > maxFileSize)
            {
                throw new InvalidPollAnswerException("Attached file is too large.");
            }

            if (config.AllowedFileTypes is { Length: > 0 } allowedTypes)
            {
                var contentType = MediaConfig.ResolveContentType(media.Extension);

                if (contentType is null || !allowedTypes.Contains(contentType))
                {
                    throw new InvalidPollAnswerException(
                        "Attached file is of an unsupported type."
                    );
                }
            }

            resolved.Add(media);
        }

        return resolved;
    }

    public async Task UpdatePollAnswerAsync(
        Ulid answerId,
        JsonPatchDocument<UpdatePollAnswerDTO> updateAnswerDto
    )
    {
        var model = await _pollAnswerRepository.GetByIdAsync(answerId);

        model.ThrowNotFoundIfNull();

        _jsonPatchUpdateService.ApplyPatch(model, updateAnswerDto);
    }

    public async Task UpdatePollAsync(Ulid id, JsonPatchDocument<UpdatePollDTO> updatePollDto)
    {
        var model = await _pollRepository.GetWithQuestionsForUpdateAsync(id);

        model.ThrowNotFoundIfNull();

        _jsonPatchUpdateService.ApplyPatch(model, updatePollDto);
    }

    private Task<bool> UserAlreadyParticipatedAsync(
        Ulid pollId,
        Ulid? userId,
        string? userExternalIdentifier
    )
    {
        return _pollParticipationRepository.ParticipationExistsAsync(
            pollId,
            userId,
            userExternalIdentifier
        );
    }
}
