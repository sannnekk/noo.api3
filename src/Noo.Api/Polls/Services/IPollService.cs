using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Polls.DTO;
using Noo.Api.Polls.Filters;
using Noo.Api.Polls.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Polls.Services;

public interface IPollService
{
    public Ulid CreatePoll(CreatePollDTO createPollDto);
    public Task UpdatePollAsync(Ulid id, JsonPatchDocument<UpdatePollDTO> updatePollDto);
    public void DeletePoll(Ulid id);
    public Task<PollModel?> GetPollAsync(Ulid id);
    public Task<SearchResult<PollModel>> GetPollsAsync(PollFilter filter);
    public Task ParticipateAsync(Ulid pollId, CreatePollParticipationDTO participationDto);
    public Task<SearchResult<PollParticipationModel>> GetPollParticipationsAsync(
        Ulid pollId,
        PollParticipationFilter filter
    );
    public Task<PollParticipationModel?> GetPollParticipationAsync(Ulid participationId);
    public Task UpdatePollAnswerAsync(
        Ulid answerId,
        JsonPatchDocument<UpdatePollAnswerDTO> updateAnswerDto
    );
}
