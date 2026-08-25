namespace Noo.Api.Courses.Services;

public interface ICourseAccessService
{
    /// <summary>
    /// Whether a student may open a course, whether through an explicit membership or because the
    /// course is open to everyone.
    /// </summary>
    public Task<bool> HasAccessAsync(Ulid courseId, Ulid studentId);
}
