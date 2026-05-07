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
}
