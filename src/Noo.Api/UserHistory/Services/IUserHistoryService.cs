using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.UserHistory.Filters;
using Noo.Api.UserHistory.Models;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.UserHistory.Services;

public interface IUserHistoryService
{
    public void Record(
        Ulid subjectUserId,
        Ulid? actorUserId,
        UserHistoryType type,
        Dictionary<string, string>? payload = null
    );

    public Task<SearchResult<UserHistoryModel>> GetHistoryAsync(
        Ulid userId,
        UserHistoryPerspective perspective,
        UserHistoryFilter filter
    );
}
