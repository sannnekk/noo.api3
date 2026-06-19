using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.NooTube.Models;

namespace Noo.Api.NooTube.Services;

public interface IVideoReactionRepository : IRepository<NooTubeVideoReactionModel>
{
    public Task<NooTubeVideoReactionModel?> GetAsync(Ulid videoId, Ulid userId);

    public void Delete(Ulid videoId, Ulid userId);
}
