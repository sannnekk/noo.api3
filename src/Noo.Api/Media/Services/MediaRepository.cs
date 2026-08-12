using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Media.Models;

namespace Noo.Api.Media.Services;

[RegisterScoped(typeof(IMediaRepository))]
public class MediaRepository : Repository<MediaModel>, IMediaRepository
{
    public MediaRepository(NooDbContext context) : base(context)
    {
    }

    public Task<List<MediaModel>> GetByIdsAsync(IEnumerable<Ulid> ids)
    {
        var wanted = ids.ToArray();

        return Context.GetDbSet<MediaModel>()
            .Where(media => wanted.Contains(media.Id))
            .ToListAsync();
    }
}
