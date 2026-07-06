using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Services;

[RegisterScoped(typeof(ICourseRepository))]
public class CourseRepository : Repository<CourseModel>, ICourseRepository
{
    public CourseRepository(NooDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<CourseModel?> GetWithChapterTreeAsync(Ulid courseId, bool includeInactive = false, Ulid? reactionsUserId = null, int maxDepth = CourseConfig.MaxChapterTreeDepth)
    {
        var chaptersQuery = Context.GetDbSet<CourseChapterModel>()
            .AsNoTracking()
            .Where(c => c.CourseId == courseId)
            .Where(c => includeInactive || c.IsActive);

        IQueryable<CourseChapterModel> chaptersWithMaterials = reactionsUserId is Ulid userId
            ? chaptersQuery
                .Include(c => c.Materials.Where(m => includeInactive || m.IsActive).OrderBy(m => m.Order))
                .ThenInclude(m => m.Reactions!.Where(r => r.UserId == userId))
            : chaptersQuery
                .Include(c => c.Materials.Where(m => includeInactive || m.IsActive).OrderBy(m => m.Order));

        var chapters = await chaptersWithMaterials.ToListAsync();

        var course = await Context.GetDbSet<CourseModel>()
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Include(c => c.Subject)
            .Include(c => c.Thumbnail)
            .FirstOrDefaultAsync();

        if (course is null)
        {
            return null;
        }

        return BuildTree(course, chapters, maxDepth);
    }

    public Task<bool> MaterialExistsAsync(Ulid courseId, Ulid materialId)
    {
        return Context.GetDbSet<CourseMaterialModel>()
            .AnyAsync(m => m.Id == materialId && m.Chapter.CourseId == courseId);
    }

    public async Task<CourseModel?> GetWithChapterTreeForUpdateAsync(Ulid courseId)
    {
        var course = await Context.GetDbSet<CourseModel>()
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course is null)
        {
            return null;
        }

        // Load every chapter and its materials tracked. EF relationship fix-up wires the
        // in-memory graph (course.Chapters, each chapter.SubChapters / .Materials). The
        // update merge then reconciles the tree purely through ParentChapterId / ChapterId
        // without reassigning any tracked navigation collection, so no existing child is
        // ever severed from its required parent and orphan-deleted before the patch runs.
        await Context.GetDbSet<CourseChapterModel>()
            .Where(c => c.CourseId == courseId)
            .Include(c => c.Materials)
            .LoadAsync();

        return course;
    }

    private CourseModel BuildTree(CourseModel course, List<CourseChapterModel> chapters, int maxDepth)
    {
        course.Chapters = chapters
            .Where(c => c.ParentChapterId == null)
            .Select(c => BuildSubTree(c, chapters, maxDepth))
            .OrderBy(c => c.Order)
            .ToList();

        return course;
    }

    private CourseChapterModel BuildSubTree(CourseChapterModel chapter, List<CourseChapterModel> chapters, int maxDepth, int currentDepth = 0)
    {
        if (currentDepth >= maxDepth)
        {
            return chapter;
        }

        chapter.SubChapters = chapters
            .Where(c => c.ParentChapterId == chapter.Id)
            .Select(c => BuildSubTree(c, chapters, maxDepth, currentDepth + 1))
            .OrderBy(c => c.Order)
            .ToList();

        return chapter;
    }
}
