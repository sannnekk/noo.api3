using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Services;

public interface ICourseRepository : IRepository<CourseModel>
{
    /// <summary>
    /// Loads a course and its chapter tree for read-only display. The result is
    /// not tracked, so the manual tree-shaping cannot corrupt EF change tracking.
    /// </summary>
    public Task<CourseModel?> GetWithChapterTreeAsync(Ulid courseId, bool includeInactive = false, int maxDepth = CourseConfig.MaxChapterTreeDepth);

    /// <summary>
    /// Loads a course and its chapter tree tracked, shaped so the top-level
    /// <see cref="CourseModel.Chapters"/> collection holds only root chapters and
    /// each chapter's sub-chapters/materials hang off it. EF owns the navigation
    /// collections (via filtered includes), so applying a JSON Patch and saving
    /// does not orphan existing children.
    /// </summary>
    public Task<CourseModel?> GetWithChapterTreeForUpdateAsync(Ulid courseId);
}
