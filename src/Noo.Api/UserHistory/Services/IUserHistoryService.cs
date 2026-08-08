using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.UserHistory.Filters;
using Noo.Api.UserHistory.Models;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.UserHistory.Services;

public interface IUserHistoryService
{
    /// <summary>
    /// Writes one entry to a user's history.
    ///
    /// The entry joins the current unit of work; it is persisted when that unit of work commits.
    /// <paramref name="actorUserId"/> is who performed the action, null when the subject acted on
    /// their own behalf. <paramref name="payload"/> holds display data captured now, so the entry
    /// survives renames and deletions of whatever it refers to.
    /// </summary>
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
