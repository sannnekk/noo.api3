namespace Noo.Api.Courses.Types;

/// <summary>
/// Which population a <c>course_audience</c> row opens a course to. One row covers the whole
/// population, so making a course public never touches per-student tables.
/// </summary>
public enum CourseAudienceKind
{
    Everyone,

    /// <summary>
    /// Reserved for subscriptions. No rule evaluates it yet, so such a row grants nothing.
    /// </summary>
    SubscriptionTier,
}
