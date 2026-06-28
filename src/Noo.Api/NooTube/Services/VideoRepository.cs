using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.NooTube.Models;
using Noo.Api.NooTube.Types;

namespace Noo.Api.NooTube.Services;

[RegisterScoped(typeof(IVideoRepository))]
public class VideoRepository : Repository<NooTubeVideoModel>, IVideoRepository
{
    public VideoRepository(NooDbContext dbContext)
        : base(dbContext) { }

    public Task<NooTubeVideoModel?> GetByExternalIdAsync(
        NooTubeServiceType serviceType,
        string externalId
    )
    {
        return Context
            .GetDbSet<NooTubeVideoModel>()
            .FirstOrDefaultAsync(v =>
                v.ServiceType == serviceType && v.ExternalIdentifier == externalId
            );
    }

    public Task<NooTubeVideoModel?> GetVideoAsync(Ulid videoId, Ulid? currentUserId)
    {
        return Context
            .GetDbSet<NooTubeVideoModel>()
            .Include(v => v.UploadedBy)
            .Include(v => v.Favourites.Where(f => f.UserId == currentUserId))
            .FirstOrDefaultAsync(v => v.Id == videoId);
    }
}
