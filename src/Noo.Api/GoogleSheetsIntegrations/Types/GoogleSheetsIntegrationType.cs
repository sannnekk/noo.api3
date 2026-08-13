namespace Noo.Api.GoogleSheetsIntegrations.Types;

public enum GoogleSheetsIntegrationType
{
    /// <summary>
    /// Users, optionally by role, course and registration date range.
    /// </summary>
    Users,

    /// <summary>
    /// Courses, optionally by subject and creation date range.
    /// </summary>
    Courses,

    /// <summary>
    /// Answers to a single poll, one column per question.
    /// </summary>
    PollResults,

    /// <summary>
    /// Assigned works of one student, or of one mentor across their students.
    /// </summary>
    AssignedWorks,
}
