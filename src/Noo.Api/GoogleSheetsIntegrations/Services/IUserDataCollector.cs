using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.ThirdPartyServices.Google;

namespace Noo.Api.GoogleSheetsIntegrations.Services;

public interface IUserDataCollector
{
    public Task<DataTable> GetUsersFromCourseAsync(Ulid courseId);
    public Task<DataTable> GetUsersFromWorkAsync(Ulid workId);
    public Task<DataTable> GetUsersFromRoleAsync(UserRoles role);
}
