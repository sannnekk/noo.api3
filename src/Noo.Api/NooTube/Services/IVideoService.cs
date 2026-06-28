using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.NooTube.DTO;
using Noo.Api.NooTube.Filters;
using Noo.Api.NooTube.Models;
using Noo.Api.NooTube.Types;
using SystemTextJsonPatch;

namespace Noo.Api.NooTube.Services;

public interface IVideoService
{
    public Task<SearchResult<NooTubeVideoModel>> GetAsync(VideoFilter filter);

    public Task<NooTubeVideoModel?> GetByIdAsync(Ulid videoId);

    public Task<NooTubeVideoUploadDTO> CreateAsync(CreateNooTubeVideoDTO createDto);

    public Task FinishUploadAsync(Ulid videoId);

    public Task UpdateEngineStatusAsync(
        NooTubeServiceType serviceType,
        string externalId,
        string? rawStatus
    );

    public Task UpdateAsync(Ulid videoId, JsonPatchDocument<UpdateNooTubeVideoDTO> patch);

    public Task ToggleFavouriteAsync(Ulid videoId);

    public Task DeleteAsync(Ulid videoId);

    public Task ToggleReactionAsync(Ulid videoId, VideoReaction reaction);
}
