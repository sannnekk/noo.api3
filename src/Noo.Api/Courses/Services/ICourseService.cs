using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Filters;
using Noo.Api.Courses.Models;
using Noo.Api.Courses.Types;
using SystemTextJsonPatch;

namespace Noo.Api.Courses.Services;

public interface ICourseService
{
    public Task<Ulid> CreateAsync(CreateCourseDTO dto);
    public Task<Ulid> CreateMaterialContentAsync(CreateCourseMaterialContentDTO dto);
    public Task<CourseModel?> GetByIdAsync(Ulid id, bool includeInactive);
    public Task<CourseMaterialContentModel?> GetContentByIdAsync(Ulid contentId);
    public Task<SearchResult<CourseModel>> SearchAsync(CourseFilter filter);

    /// <summary>
    /// The current student's own course list, covering both assigned and publicly open courses.
    /// </summary>
    public Task<SearchResult<CourseModel>> SearchOwnAsync(StudentCourseFilter filter);
    public Task SetArchivedAsync(Ulid courseId, bool isArchived);
    public Task SoftDeleteAsync(Ulid courseId);
    public Task ToggleMaterialReactionAsync(Ulid courseId, Ulid materialId, CourseMaterialReactionTypes reaction);
    public Task UpdateAsync(Ulid courseId, JsonPatchDocument<UpdateCourseDTO> courseUpdateDto);
    public Task UpdateContentAsync(Ulid contentId, JsonPatchDocument<UpdateCourseMaterialContentDTO> contentUpdateDto);

    /// <summary>
    /// Patches a material's content, giving the material one if it has none yet. Addressed by
    /// material because a material being edited together has an id before its content exists,
    /// and scoped to the course so an operation cannot reach a material outside it.
    ///
    /// False when the course has no such material — which a shared draft can name, because
    /// somebody may have removed it before anyone pressed Save.
    /// </summary>
    public Task<bool> UpdateMaterialContentAsync(Ulid courseId, Ulid materialId, JsonPatchDocument<UpdateCourseMaterialContentDTO> contentUpdateDto);
}
