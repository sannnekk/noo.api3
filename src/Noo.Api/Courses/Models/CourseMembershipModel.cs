using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Courses.Types;
using Noo.Api.Users.Models;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.Courses.Models;

/// <summary>
/// One student's place on one course. A student holds at most one of these per course —
/// leaving and coming back reuses the same row rather than adding another — which the
/// unique index below is what actually guarantees. It answers only "may this student enter";
/// how they want the course displayed lives in <see cref="CourseStudentStateModel"/>.
/// </summary>
[Model("course_membership")]
[Index(nameof(CourseId), nameof(StudentId), IsUnique = true)]
public class CourseMembershipModel : BaseModel
{
    [Required]
    [Column("is_active", TypeName = DbDataTypes.Boolean)]
    public bool IsActive { get; set; }

    [Column("is_archived", TypeName = DbDataTypes.Boolean)]
    public bool IsArchived { get; set; }

    [Column("course_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Course))]
    public Ulid CourseId { get; set; }

    [Column("student_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Student))]
    public Ulid StudentId { get; set; }

    [Column("assigner_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Assigner))]
    public Ulid? AssignerId { get; set; }

    [Column("type", TypeName = CourseEnumDbDataTypes.CourseMembershipType)]
    public CourseMembershipType Type { get; init; }

    #region Navigation Properties

    [DeleteBehavior(DeleteBehavior.Cascade)]
    [InverseProperty(nameof(CourseModel.Memberships))]
    public CourseModel Course { get; set; } = default!;

    [DeleteBehavior(DeleteBehavior.Cascade)]
    [InverseProperty(nameof(UserModel.CoursesAsMember))]
    public UserModel Student { get; set; } = default!;

    [DeleteBehavior(DeleteBehavior.SetNull)]
    [InverseProperty(nameof(UserModel.CoursesAsAssigner))]
    public UserModel? Assigner { get; set; }

    #endregion
}
