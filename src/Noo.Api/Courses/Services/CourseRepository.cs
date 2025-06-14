using System.Data.Entity;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Services;

public class CourseRepository : Repository<CourseModel>, ICourseRepository
{
    public async Task<CourseModel?> GetWithChapterTreeAsync(CourseModel course, int maxDepth = 2)
    {
        var courseId = course.Id;

        var chapters = await Context.GetDbSet<CourseChapterModel>()
            .Where(c => c.CourseId == courseId)
            .Include(c => c.Materials)
            .ToListAsync();

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

public static class IUnitOfWorkCourseRepositoryExtensions
{
    public static ICourseRepository CourseRepository(this IUnitOfWork unitOfWork)
    {
        return new CourseRepository()
        {
            Context = unitOfWork.Context
        };
    }
}

