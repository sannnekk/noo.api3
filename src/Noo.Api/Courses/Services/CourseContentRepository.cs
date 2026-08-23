using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Models;
using Noo.Api.Media.Models;

namespace Noo.Api.Courses.Services;

[RegisterScoped(typeof(ICourseContentRepository))]
public class CourseContentRepository : Repository<CourseMaterialContentModel>, ICourseContentRepository
{
    public CourseContentRepository(NooDbContext dbContext) : base(dbContext)
    {
    }

    public Task<CourseMaterialContentModel?> GetAsync(Ulid contentId)
    {
        return Context.GetDbSet<CourseMaterialContentModel>()
            .Include(c => c.Medias)
            .Include(c => c.Poll)
            .Include(c => c.NooTubeVideos)
            .Include(c => c.WorkAssignments)
            .ThenInclude(wa => wa.Work)
            .ThenInclude(w => w.Subject)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == contentId);
    }

    // A chapter carries its course id directly, so the nested chapter tree needs no walking.
    public Task<Ulid?> GetCourseIdByContentIdAsync(Ulid contentId)
    {
        return Context.GetDbSet<CourseMaterialModel>()
            .Where(m => m.ContentId == contentId)
            .Select(m => (Ulid?)m.Chapter.CourseId)
            .FirstOrDefaultAsync();
    }

    public Task<Ulid?> GetMaterialIdByMediaIdAsync(Ulid mediaId)
    {
        return Context.GetDbSet<CourseMaterialContentModel>()
            .Where(c => c.Medias!.Any(m => m.Id == mediaId))
            .Select(c => (Ulid?)c.Material.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<MediaModel>?> GetMaterialMediaAsync(Ulid materialId)
    {
        var material = await Context.GetDbSet<CourseMaterialModel>()
            .Include(m => m.Content)
            .ThenInclude(c => c.Medias)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == materialId);

        if (material is null)
        {
            return null;
        }

        return material.Content?.Medias?.ToList() ?? [];
    }
}
