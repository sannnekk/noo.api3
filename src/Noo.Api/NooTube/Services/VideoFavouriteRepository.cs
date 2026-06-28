using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.NooTube.Models;

namespace Noo.Api.NooTube.Services;

[RegisterScoped(typeof(IVideoFavouriteRepository))]
public class VideoFavouriteRepository
    : Repository<NooTubeVideoFavouriteModel>,
        IVideoFavouriteRepository
{
    public VideoFavouriteRepository(NooDbContext dbContext)
        : base(dbContext) { }

    public void Delete(Ulid videoId, Ulid userId)
    {
        DeleteEntity(new() { VideoId = videoId, UserId = userId });
    }

    public Task<NooTubeVideoFavouriteModel?> GetAsync(Ulid videoId, Ulid userId)
    {
        return Context
            .GetDbSet<NooTubeVideoFavouriteModel>()
            .Where(f => f.VideoId == videoId && f.UserId == userId)
            .FirstOrDefaultAsync();
    }
}
