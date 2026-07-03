using Noo.Api.Courses.Models;
using Noo.Api.Subjects.Models;

namespace Noo.Api.Works.Types;

public class WorkRelation
{
    public Ulid CourseId { get; set; }

    public Ulid MaterialId { get; set; }

    public SubjectModel? Subject { get; set; }

    /// <summary>
    /// The path of the work in a course tree, format:
    /// "Course Name / Chapter name / ... / Material name"
    /// </summary>
    public IEnumerable<string> Path { get; set; } = null!;

    public CourseWorkAssignmentModel Assignment { get; set; } = null!;
}
