using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Platform.DTO;
using Noo.Api.Platform.Types;

namespace Noo.Api.Platform.Services;

[RegisterScoped(typeof(IPlatformService))]
public class PlatformService : IPlatformService
{
    public string GetPlatformVersion()
    {
        return NooApiVersions.Current;
    }

    public SearchResult<ChangeLogDTO> GetChangelog()
    {
        // TODO: Replace with actual changelog retrieval logic
        return new SearchResult<ChangeLogDTO>([
            new ChangeLogDTO
            {
                Version = NooApiVersions.Current,
                Date = DateTime.UtcNow,
                Changes = [
                    new PlatformChange
                    {
                        Type = ChangeType.Feature,
                        Author = "Noo Team",
                        Description = "Initial release of the Noo API platform."
                    },
                    new PlatformChange
                    {
                        Type = ChangeType.BugFix,
                        Author = "Noo Team",
                        Description = "Updated API documentation and versioning."
                    },
                    new PlatformChange
                    {
                        Type = ChangeType.Optimization,
                        Author = "Noo Team",
                        Description = "Deprecated old endpoints in favor of new ones."
                    },
                    new PlatformChange
                    {
                        Type = ChangeType.Refactor,
                        Author = "Noo Team",
                        Description = "Refactored API controllers for better maintainability."
                    }
                ]
            }
        ], 1);
    }
}
