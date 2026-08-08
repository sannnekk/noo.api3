using Ardalis.Specification;
using Noo.Api.UserHistory.Models;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.UserHistory.Specifications;

/// <summary>
/// Narrows the log to one user, on the side named by the perspective.
/// </summary>
/// <remarks>
/// The user predicate lives here rather than on the filter because AutoFilterer cannot compare
/// against a nullable column: filtering <c>ActorUserId</c> through it throws
/// "Nullable object must have a value". Keeping it here also means a client cannot widen the
/// query by sending a user id of its own.
///
/// Only ever one of the two columns is compared — an OR across both would defeat the composite
/// indexes that keep this query's cost independent of the table's size.
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
