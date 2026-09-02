using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Platform.Models;

namespace Noo.Api.Platform.Services;

[RegisterScoped(typeof(IPlatformSettingsRepository))]
public class PlatformSettingsRepository
    : Repository<PlatformSettingsModel>,
        IPlatformSettingsRepository
{
    public PlatformSettingsRepository(NooDbContext dbContext)
        : base(dbContext) { }

    public Task<PlatformSettingsModel?> GetSingletonAsync()
    {
        return GetByIdAsync(PlatformSettingsModel.SingletonId);
    }

    public async Task<PlatformSettingsModel> GetOrCreateSingletonAsync()
    {
        var settings = await GetSingletonAsync();

        if (settings is null)
        {
            settings = new PlatformSettingsModel { Id = PlatformSettingsModel.SingletonId };

            Add(settings);
        }

        return settings;
    }
}
