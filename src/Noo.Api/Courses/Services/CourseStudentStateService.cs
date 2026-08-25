using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Courses.Services;

[RegisterScoped(typeof(ICourseStudentStateService))]
public class CourseStudentStateService : ICourseStudentStateService
{
    private readonly NooDbContext _db;

    private readonly IJsonPatchUpdateService _jsonPatchUpdateService;

    public CourseStudentStateService(
        NooDbContext db,
        IJsonPatchUpdateService jsonPatchUpdateService
    )
    {
        _db = db;
        _jsonPatchUpdateService = jsonPatchUpdateService;
    }

    public async Task PatchStateAsync(
        Ulid courseId,
        Ulid studentId,
        JsonPatchDocument<UpdateCourseStudentStateDTO> patch
    )
    {
        var state = await GetOrCreateAsync(courseId, studentId);

        _jsonPatchUpdateService.ApplyPatch(state, patch);
    }

    private async Task<CourseStudentStateModel> GetOrCreateAsync(Ulid courseId, Ulid studentId)
    {
        var set = _db.GetDbSet<CourseStudentStateModel>();

        // A student holds at most one state row per course (unique index), so an already-tracked
        // instance must be reused rather than added a second time.
        var state =
            set.Local.FirstOrDefault(s => s.CourseId == courseId && s.StudentId == studentId)
            ?? await set.FirstOrDefaultAsync(s =>
                s.CourseId == courseId && s.StudentId == studentId
            );

        if (state != null)
        {
            return state;
        }

        state = new CourseStudentStateModel { CourseId = courseId, StudentId = studentId };

        set.Add(state);

        return state;
    }
}
