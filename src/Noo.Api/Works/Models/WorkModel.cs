using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Courses.Models;
using Noo.Api.Subjects.Models;
using Noo.Api.Works.Types;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.Works.Models;

[Model("work")]
[Index(nameof(Title), IsUnique = false)]
[Index(nameof(Type), IsUnique = false)]
public class WorkModel : BaseModel
{
    [Required]
    [MinLength(1)]
    [MaxLength(255)]
    [Column("title", TypeName = DbDataTypes.Varchar255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("type", TypeName = WorkEnumDbDataTypes.WorkType)]
    public WorkType Type { get; set; }

    [MaxLength(255)]
    [Column("description", TypeName = DbDataTypes.Varchar255)]
    public string? Description { get; set; }

    [Column("subject_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Subject))]
    public Ulid? SubjectId { get; set; }

    #region Navigation Properties

    [InverseProperty(nameof(WorkTaskModel.Work))]
    public ICollection<WorkTaskModel>? Tasks { get; set; }

    [DeleteBehavior(DeleteBehavior.SetNull)]
    [InverseProperty(nameof(SubjectModel.Works))]
    public SubjectModel? Subject { get; set; }

    public ICollection<CourseWorkAssignmentModel> CourseWorkAssignments { get; set; } = [];

    #endregion
}
