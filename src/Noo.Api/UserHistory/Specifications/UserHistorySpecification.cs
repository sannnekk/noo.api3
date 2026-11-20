using Ardalis.Specification;
using Noo.Api.UserHistory.Models;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.UserHistory.Specifications;

/// <summary>
/// Narrows the log to one user on the side named by the perspective
/// </summary>
/// <remarks>
/// The user predicate lives here rather than on the filter because AutoFilterer cannot compare
/// against a nullable column: filtering <c>ActorUserId</c> through it throws
/// "Nullable object must have a value"
/// </remarks>
public class UserHistorySpecification : Specification<UserHistoryModel>
{
    public UserHistorySpecification(Ulid userId, UserHistoryPerspective perspective)
    {
        if (perspective == UserHistoryPerspective.Actor)
        {
            Query.Where(x => x.ActorUserId == userId);
        }
        else
        {
            Query.Where(x => x.SubjectUserId == userId);
        }

        Query.Include(x => x.Actor);
    }
}
