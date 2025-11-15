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

    public async Task<CourseModel?> GetWithChapterTreeAsync(Ulid courseId, bool includeInactive = false, int maxDepth = 2)
    {
        var chapters = await Context.GetDbSet<CourseChapterModel>()
            .Where(c => c.CourseId == courseId)
            .Where(c => includeInactive || c.IsActive)
            .Include(c => c.Materials.Where(m => includeInactive || m.IsActive))
            .ToListAsync();

        var course = await Context.GetDbSet<CourseModel>()
            .Where(c => c.Id == courseId)
            .FirstOrDefaultAsync();

        if (course is null)
        {
            return null;
        }

        return BuildTree(course, chapters, maxDepth);
    }

    private CourseModel BuildTree(CourseModel course, List<CourseChapterModel> chapters, int maxDepth)
    {
        course.Chapters = chapters
            .Where(c => c.ParentChapterId == null)
            .Select(c => BuildSubTree(c, chapters, maxDepth))
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
            .ToList();

        return chapter;
    }
}
