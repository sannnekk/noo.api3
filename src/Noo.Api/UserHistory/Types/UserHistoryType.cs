namespace Noo.Api.UserHistory.Types;

/// <summary>
/// The kinds of event recorded in a user's activity log.
/// Stored as a string rather than a MySQL ENUM, so adding a kind needs no schema migration
/// </summary>
public enum UserHistoryType
{
    // Account lifecycle
    Registered,
    EmailConfirmed,
    EmailChanged,
    PasswordChanged,
    PasswordReset,
    ProfileUpdated,

    // Administrative actions
    RoleChanged,
    Blocked,
    Unblocked,
    Verified,

    // Courses and mentors
    AddedToCourse,
    RemovedFromCourse,
    MentorAssigned,
    MentorUnassigned,

    // Assigned works
    WorkAssigned,
    WorkSolved,
    WorkChecked,
    WorkSentOnRecheck,
    WorkSentOnResolve,
}
