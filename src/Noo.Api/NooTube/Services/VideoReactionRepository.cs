using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.NooTube.Models;
using Noo.Api.NooTube.Types;

namespace Noo.Api.NooTube.Services;

[RegisterScoped(typeof(IVideoReactionRepository))]
public class VideoReactionRepository
    : Repository<NooTubeVideoReactionModel>,
        IVideoReactionRepository
{
    public VideoReactionRepository(NooDbContext dbContext)
        : base(dbContext) { }

    public Task<NooTubeVideoReactionModel?> GetAsync(Ulid videoId, Ulid userId)
    {
        return Context
            .GetDbSet<NooTubeVideoReactionModel>()
            .Where(r => r.VideoId == videoId && r.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyDictionary<VideoReaction, int>> GetCountsAsync(Ulid videoId)
    {
        var counts = await Context
            .GetDbSet<NooTubeVideoReactionModel>()
            .AsNoTracking()
            .Where(r => r.VideoId == videoId)
            .GroupBy(r => r.Reaction)
            .Select(group => new { Reaction = group.Key, Count = group.Count() })
            .ToListAsync();

        return counts.ToDictionary(entry => entry.Reaction, entry => entry.Count);
    }
}
