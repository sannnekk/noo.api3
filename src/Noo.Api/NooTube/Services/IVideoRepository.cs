using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.NooTube.Models;
using Noo.Api.NooTube.Types;

namespace Noo.Api.NooTube.Services;

public interface IVideoRepository : IRepository<NooTubeVideoModel>
{
    public Task<NooTubeVideoModel?> GetByExternalIdAsync(
        NooTubeServiceType serviceType,
        string externalId
    );
}
