using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Users.Models;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.Courses.Models;

/// <summary>
/// How one student wants one course displayed. Deliberately separate from
/// <see cref="CourseMembershipModel"/>: this row is created only when the student first pins or
/// archives, and it says nothing about access, so a course losing its public audience leaves these
/// rows harmlessly behind instead of needing a cleanup.
/// </summary>
[Model("course_student_state")]
[Index(nameof(CourseId), nameof(StudentId), IsUnique = true)]
public class CourseStudentStateModel : BaseModel
{
    [Column("course_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Course))]
    public Ulid CourseId { get; set; }

    [Column("student_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Student))]
    public Ulid StudentId { get; set; }

    [Column("is_pinned", TypeName = DbDataTypes.Boolean)]
    public bool IsPinned { get; set; }

    [Column("is_archived", TypeName = DbDataTypes.Boolean)]
    public bool IsArchived { get; set; }

    #region Navigation Properties

    [DeleteBehavior(DeleteBehavior.Cascade)]
    [InverseProperty(nameof(CourseModel.StudentStates))]
    public CourseModel Course { get; set; } = default!;

    [DeleteBehavior(DeleteBehavior.Cascade)]
    [InverseProperty(nameof(UserModel.CourseStates))]
    public UserModel Student { get; set; } = default!;

    #endregion
}
