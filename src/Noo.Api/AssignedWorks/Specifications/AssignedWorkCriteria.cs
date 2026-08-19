using System.Linq.Expressions;
using Noo.Api.AssignedWorks.Models;

namespace Noo.Api.AssignedWorks.Specifications;

public static class AssignedWorkCriteria
{
    /// <summary>
    /// The works the given user takes part in, as a student or as either of the mentors.
    /// The query form of <see cref="AssignedWorkModel.IsParticipant"/>; the two must agree,
    /// which <c>AssignedWorkCriteriaTests</c> checks.
    /// </summary>
    public static Expression<Func<AssignedWorkModel, bool>> ParticipatedBy(Ulid userId) =>
        aw =>
            aw.StudentId == userId || aw.MainMentorId == userId || aw.HelperMentorId == userId;
}
