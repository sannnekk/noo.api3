using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Platform.Models;

namespace Noo.Api.Platform.Services;

public interface IPlatformSettingsRepository : IRepository<PlatformSettingsModel>
{
    /// <summary>
    /// The stored settings, or <c>null</c> when nobody has saved any yet.
    /// </summary>
    public Task<PlatformSettingsModel?> GetSingletonAsync();

    /// <summary>
    /// The stored settings, creating the row from the model defaults on the
    /// first save. Only the update path uses this — reads must not write.
    /// </summary>
    public Task<PlatformSettingsModel> GetOrCreateSingletonAsync();
}
