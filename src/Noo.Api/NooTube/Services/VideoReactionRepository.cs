using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.NooTube.Models;

namespace Noo.Api.NooTube.Services;

[RegisterScoped(typeof(IVideoReactionRepository))]
public class VideoReactionRepository
    : Repository<NooTubeVideoReactionModel>,
        IVideoReactionRepository
{
    public VideoReactionRepository(NooDbContext dbContext)
        : base(dbContext) { }

    public void Delete(Ulid videoId, Ulid userId)
    {
        DeleteEntity(new() { VideoId = videoId, UserId = userId });
    }

    public Task<NooTubeVideoReactionModel?> GetAsync(Ulid videoId, Ulid userId)
    {
        return Context
            .GetDbSet<NooTubeVideoReactionModel>()
            .Where(r => r.VideoId == videoId && r.UserId == userId)
            .FirstOrDefaultAsync();
    }
}
