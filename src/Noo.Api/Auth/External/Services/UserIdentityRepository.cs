using Microsoft.EntityFrameworkCore;
using Noo.Api.Auth.External.Models;
using Noo.Api.Auth.External.Types;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Auth.External.Services;

[RegisterScoped(typeof(IUserIdentityRepository))]
public class UserIdentityRepository : Repository<UserIdentityModel>, IUserIdentityRepository
{
    public UserIdentityRepository(NooDbContext context) : base(context) { }

    public Task<UserIdentityModel?> GetByProviderAndSubjectAsync(
        ExternalAuthProviderType provider,
        string subjectId
    )
    {
        return Context
            .GetDbSet<UserIdentityModel>()
            .Include(identity => identity.User)
            .FirstOrDefaultAsync(identity =>
                identity.Provider == provider && identity.SubjectId == subjectId
            );
    }

    public async Task<IReadOnlyList<UserIdentityModel>> GetByUserAsync(Ulid userId)
    {
        return await Context
            .GetDbSet<UserIdentityModel>()
            .Where(identity => identity.UserId == userId)
            .OrderBy(identity => identity.Id)
            .ToListAsync();
    }

    public Task<int> CountByUserAsync(Ulid userId)
    {
        return Context
            .GetDbSet<UserIdentityModel>()
            .CountAsync(identity => identity.UserId == userId);
    }
}
