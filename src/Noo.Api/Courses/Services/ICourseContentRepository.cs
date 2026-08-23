using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.Models;
using Noo.Api.Media.Models;

namespace Noo.Api.Courses.Services;

public interface ICourseContentRepository : IRepository<CourseMaterialContentModel>
{
    public Task<CourseMaterialContentModel?> GetAsync(Ulid contentId);

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
