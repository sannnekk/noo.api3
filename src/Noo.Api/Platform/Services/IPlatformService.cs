using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Platform.DTO;
using Noo.Api.Platform.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Platform.Services;

public interface IPlatformService
{
    public string GetPlatformVersion();

    public SearchResult<ChangeLogDTO> GetChangelog();

    /// <summary>
    /// The platform's links and contacts. Never null: with no row saved yet, the
    /// defaults the frontend shipped with are returned without persisting them.
    /// </summary>
    public Task<PlatformSettingsModel> GetSettingsAsync();

    public Task UpdateSettingsAsync(JsonPatchDocument<UpdatePlatformSettingsDTO> dto);
}
