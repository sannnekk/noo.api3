using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Polls.DTO;
using Noo.Api.Polls.Exceptions;
using Noo.Api.Polls.Filters;
using Noo.Api.Polls.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Polls.Services;

[RegisterScoped(typeof(IPollService))]
public class PollService : IPollService
{
    private readonly IMapper _mapper;
    private readonly IPollRepository _pollRepository;
    private readonly IPollParticipationRepository _pollParticipationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPollAnswerRepository _pollAnswerRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IJsonPatchUpdateService _jsonPatchUpdateService;

    public PollService(IMapper mapper, IUnitOfWork unitOfWork, IPollRepository pollRepository, IPollParticipationRepository pollParticipationRepository, IPollAnswerRepository pollAnswerRepository, ICurrentUser currentUser, IJsonPatchUpdateService jsonPatchUpdateService)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _pollRepository = pollRepository;
        _pollParticipationRepository = pollParticipationRepository;
        _pollAnswerRepository = pollAnswerRepository;
        _jsonPatchUpdateService = jsonPatchUpdateService;
    }

    public Task<Ulid> CreatePollAsync(CreatePollDTO createPollDto)
    {
        var pollModel = _mapper.Map<PollModel>(createPollDto);
        _pollRepository.Add(pollModel);

        return _unitOfWork.CommitAsync().ContinueWith(_ => pollModel.Id);
    }

    public Task DeletePollAsync(Ulid id)
    {
        _pollRepository.DeleteById(id);
        return _unitOfWork.CommitAsync();
    }

    public Task<PollModel?> GetPollAsync(Ulid id)
    {
        return _pollRepository.GetByIdAsync(id);
    }

    public Task<PollParticipationModel?> GetPollParticipationAsync(Ulid participationId)
    {
        return _pollParticipationRepository.GetByIdAsync(participationId);
    }

    public Task<SearchResult<PollParticipationModel>> GetPollParticipationsAsync(Ulid pollId, PollParticipationFilter filter)
    {
        filter.PollId = pollId;
        return _pollParticipationRepository.SearchAsync(filter);
    }

    public Task<SearchResult<PollModel>> GetPollsAsync(PollFilter filter)
    {
        return _pollRepository.SearchAsync(filter);
    }

    public async Task ParticipateAsync(Ulid pollId, CreatePollParticipationDTO participationDto)
    {
        // Resolve current user id when available
        var currentUserId = _currentUser?.UserId;

        // Only check for duplicates when an identifier is present
        var hasUserId = currentUserId.HasValue;
        var hasExternal = !string.IsNullOrWhiteSpace(participationDto.UserExternalIdentifier);

        if ((hasUserId || hasExternal) &&
            await UserAlreadyParticipatedAsync(pollId, currentUserId, participationDto.UserExternalIdentifier))
        {
            throw new UserAlreadyVotedException();
        }

        var participationModel = _mapper.Map<PollParticipationModel>(participationDto);
        participationModel.PollId = pollId;
        // Persist the current user id if present
        if (currentUserId.HasValue)
        {
            participationModel.UserId = currentUserId.Value;
        }
        _pollParticipationRepository.Add(participationModel);

        await _unitOfWork.CommitAsync();
    }

    public async Task UpdatePollAnswerAsync(Ulid answerId, JsonPatchDocument<UpdatePollAnswerDTO> updateAnswerDto)
    {
        var model = await _pollAnswerRepository.GetByIdAsync(answerId);

        model.ThrowNotFoundIfNull();

        _jsonPatchUpdateService.ApplyPatch(model, updateAnswerDto);

        await _unitOfWork.CommitAsync();
    }

    public async Task UpdatePollAsync(Ulid id, JsonPatchDocument<UpdatePollDTO> updatePollDto)
    {
        var model = await _pollRepository.GetByIdAsync(id);

        model.ThrowNotFoundIfNull();

        _jsonPatchUpdateService.ApplyPatch(model, updatePollDto);

        await _unitOfWork.CommitAsync();
    }

    private Task<bool> UserAlreadyParticipatedAsync(Ulid pollId, Ulid? userId, string? userExternalIdentifier)
    {
        return _pollParticipationRepository
            .ParticipationExistsAsync(pollId, userId, userExternalIdentifier);
    }
}
