using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.UserSettings.Models;

namespace Noo.Api.UserSettings.Services;

[RegisterScoped(typeof(IUserSettingsRepository))]
public class UserSettingsRepository : Repository<UserSettingsModel>, IUserSettingsRepository
{
    public UserSettingsRepository(NooDbContext context) : base(context)
    {
    }

    public async Task<UserSettingsModel> GetOrCreateAsync(Ulid userId)
    {
        var settings = await Context.GetDbSet<UserSettingsModel>().FirstOrDefaultAsync(settings => settings.UserId == userId);

        if (settings is null)
        {
            settings = new UserSettingsModel
            {
                UserId = userId
            };

            Context.GetDbSet<UserSettingsModel>().Add(settings);
        }

        return settings;
    }
}
