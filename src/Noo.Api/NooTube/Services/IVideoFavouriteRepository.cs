using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.NooTube.Models;

namespace Noo.Api.NooTube.Services;

public interface IVideoFavouriteRepository : IRepository<NooTubeVideoFavouriteModel>
{
    public Task<NooTubeVideoFavouriteModel?> GetAsync(Ulid videoId, Ulid userId);

    public void Delete(Ulid videoId, Ulid userId);
}
