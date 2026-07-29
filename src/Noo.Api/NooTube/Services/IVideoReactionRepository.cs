using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.NooTube.Models;
using Noo.Api.NooTube.Types;

namespace Noo.Api.NooTube.Services;

public interface IVideoReactionRepository : IRepository<NooTubeVideoReactionModel>
{
    public Task<NooTubeVideoReactionModel?> GetAsync(Ulid videoId, Ulid userId);

    /// <summary>
    /// Counts the reactions of all users on a video, grouped by reaction.
    /// Reactions nobody picked are not part of the result.
    /// </summary>
    public Task<IReadOnlyDictionary<VideoReaction, int>> GetCountsAsync(Ulid videoId);
}
