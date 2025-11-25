using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Works.Models;

namespace Noo.Api.Courses.Models;

[Model("course_work_assignment")]
public class CourseWorkAssignmentModel : OrderedModel
{
    [Required]
    [Column("course_material_content_id", TypeName = DbDataTypes.Ulid)]
    public Ulid CourseMaterialContentId { get; set; }

    [Required]
    [Column("work_id", TypeName = DbDataTypes.Ulid)]
    public Ulid WorkId { get; set; }

    [MaxLength(255)]
    [Column("note", TypeName = DbDataTypes.Varchar255)]
    public string? Note { get; set; }

    [Required]
    [Column("is_active", TypeName = DbDataTypes.Boolean)]
    public bool IsActive { get; set; }

    // TODO: change db type to DateOnly
    [Column("deactivated_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime? DeactivatedAt { get; set; }

    // TODO: change db type to DateOnly
    [Column("solve_deadline_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateOnly? SolveDeadlineAt { get; set; }

    // TODO: change db type to DateOnly
    [Column("check_deadline_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateOnly? CheckDeadlineAt { get; set; }

    #region Navigation properties

    // TODO: consider default! or [] for collections in the whole app
    [DeleteBehavior(DeleteBehavior.Cascade)]
    [InverseProperty(nameof(CourseMaterialContentModel.WorkAssignments))]
    public CourseMaterialContentModel CourseMaterialContent { get; set; } = default!;

    [DeleteBehavior(DeleteBehavior.Cascade)]
    [InverseProperty(nameof(WorkModel.CourseWorkAssignments))]
    public WorkModel Work { get; set; } = default!;

    #endregion
}
