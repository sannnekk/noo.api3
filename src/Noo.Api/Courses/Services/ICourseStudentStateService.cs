using Noo.Api.Courses.DTO;
using SystemTextJsonPatch;

namespace Noo.Api.Courses.Services;

public interface ICourseStudentStateService
{
    /// <summary>
    /// Applies a patch to the per-student display state for a course, creating the row on first use.
    /// </summary>
    public Task PatchStateAsync(
        Ulid courseId,
        Ulid studentId,
        JsonPatchDocument<UpdateCourseStudentStateDTO> patch
    );
}
