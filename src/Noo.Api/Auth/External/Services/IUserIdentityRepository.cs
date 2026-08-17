using Noo.Api.Auth.External.Models;
using Noo.Api.Auth.External.Types;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.Auth.External.Services;

public interface IUserIdentityRepository : IRepository<UserIdentityModel>
{
    public Task<UserIdentityModel?> GetByProviderAndSubjectAsync(
        ExternalAuthProviderType provider,
        string subjectId
    );

    public Task<IReadOnlyList<UserIdentityModel>> GetByUserAsync(Ulid userId);

    public Task<int> CountByUserAsync(Ulid userId);
}
