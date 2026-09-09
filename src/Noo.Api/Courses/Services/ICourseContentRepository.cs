using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.Models;
using Noo.Api.Media.Models;

namespace Noo.Api.Courses.Services;

public interface ICourseContentRepository : IRepository<CourseMaterialContentModel>
{
    public Task<CourseMaterialContentModel?> GetAsync(Ulid contentId);

    /// <summary>
    /// One of a course's materials, with whatever content it has, tracked for update. Null when
    /// the material is not this course's — which is what keeps an operation naming somebody
    /// else's material from reaching it.
    ///
    /// Addressed by material rather than by content because a material being edited collaboratively
    /// has an id long before its content exists: the content is created the first time somebody
    /// types into it, and until then there is no content id to name.
    /// </summary>
    public Task<CourseMaterialModel?> GetMaterialWithContentForUpdateAsync(
        Ulid courseId,
        Ulid materialId
    );

    /// <summary>
    /// Resolves the course a material content belongs to, or null when the id names no content.
    /// </summary>
    public Task<Ulid?> GetCourseIdByContentIdAsync(Ulid contentId);

    /// <summary>
    /// Resolves the material a piece of media is attached to, or null when it is attached to none.
    /// </summary>
    public Task<Ulid?> GetMaterialIdByMediaIdAsync(Ulid mediaId);

    /// <summary>
    /// The files attached to a material's content. Null when no such material exists, which is
    /// what separates a missing material from one that simply has no files.
    /// </summary>
    public Task<IReadOnlyList<MediaModel>?> GetMaterialMediaAsync(Ulid materialId);
}
