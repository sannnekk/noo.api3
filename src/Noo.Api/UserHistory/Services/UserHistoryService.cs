using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.UserHistory.Filters;
using Noo.Api.UserHistory.Models;
using Noo.Api.UserHistory.Specifications;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.UserHistory.Services;

[RegisterScoped(typeof(IUserHistoryService))]
public class UserHistoryService : IUserHistoryService
{
    private readonly IUserHistoryRepository _userHistoryRepository;

    public UserHistoryService(IUserHistoryRepository userHistoryRepository)
    {
        _userHistoryRepository = userHistoryRepository;
    }

    public void Record(
        Ulid subjectUserId,
        Ulid? actorUserId,
        UserHistoryType type,
        Dictionary<string, string>? payload = null
    )
    {
        _userHistoryRepository.Add(
            new UserHistoryModel
            {
                SubjectUserId = subjectUserId,
                // An actor who is the subject adds nothing; keep it null so "done by" queries
                // return only actions on other people.
                ActorUserId = actorUserId == subjectUserId ? null : actorUserId,
                Type = type,
                Payload = payload,
            }
        );
    }

    public Task<SearchResult<UserHistoryModel>> GetHistoryAsync(
        Ulid userId,
        UserHistoryPerspective perspective,
        UserHistoryFilter filter
    )
    {
        return _userHistoryRepository.SearchAsync(
            filter,
            [new UserHistorySpecification(userId, perspective)]
        );
    }
}
