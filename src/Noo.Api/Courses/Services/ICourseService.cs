using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Filters;
using Noo.Api.Courses.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Courses.Services;

public interface ICourseService
{
    public Task<Ulid> CreateAsync(CreateCourseDTO dto);
    public Task<Ulid> CreateMaterialContentAsync(CreateCourseMaterialContentDTO dto);
    public Task<CourseModel?> GetByIdAsync(Ulid id, bool includeInactive);
    public Task<CourseMaterialContentModel?> GetContentByIdAsync(Ulid contentId);
    public Task<SearchResult<CourseModel>> SearchAsync(CourseFilter filter);
    public Task SoftDeleteAsync(Ulid courseId);
    public Task UpdateAsync(Ulid courseId, JsonPatchDocument<UpdateCourseDTO> courseUpdateDto);
    public Task UpdateContentAsync(Ulid contentId, JsonPatchDocument<UpdateCourseMaterialContentDTO> contentUpdateDto);
}
