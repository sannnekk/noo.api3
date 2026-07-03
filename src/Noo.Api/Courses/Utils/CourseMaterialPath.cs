using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Utils;

public static class CourseMaterialPath
{
    /// <summary>
    /// Builds the path of a material in a course tree, from the root down:
    /// "Course Name / Chapter name / ... / Material name".
    /// </summary>
    public static IReadOnlyList<string> Build(
        string courseName,
        IReadOnlyDictionary<Ulid, CourseChapterModel> chaptersById,
        Ulid leafChapterId,
        string materialTitle
    )
    {
        var path = new LinkedList<string>();
        path.AddFirst(materialTitle);

        var currentId = (Ulid?)leafChapterId;

        while (currentId is Ulid id && chaptersById.TryGetValue(id, out var chapter))
        {
            path.AddFirst(chapter.Title);
            currentId = chapter.ParentChapterId;
        }

        path.AddFirst(courseName);

        return [.. path];
    }
}
