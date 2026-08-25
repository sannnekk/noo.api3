namespace Noo.Api.Courses.Types;

/// <summary>
/// Why a student can reach a course. Presentation only — access itself is decided by
/// <see cref="Access.CourseAccessRules"/>.
/// </summary>
public enum CourseAccessSource
{
    Assignment,
    Public,
}
