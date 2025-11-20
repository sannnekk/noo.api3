using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.Courses.Models;

[Model("course_material")]
[Index(nameof(ContentId), IsUnique = true)]
public class CourseMaterialModel : OrderedModel
{
    [Required]
    [Column("title", TypeName = DbDataTypes.Varchar255)]
    [MinLength(1)]
    [MaxLength(255)]
    public string Title { get; set; } = default!;

    [Column("title_color", TypeName = DbDataTypes.Varchar63)]
    [MaxLength(63)]
    public string? TitleColor { get; set; }

    [Column("is_active", TypeName = DbDataTypes.Boolean)]
    [Required]
    public bool IsActive { get; set; }

    [Column("publish_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime? PublishAt { get; set; }

    [Column("chapter_id", TypeName = DbDataTypes.Ulid)]
    [Required]
    [ForeignKey(nameof(Chapter))]
    public Ulid ChapterId { get; set; }

    [Column("content_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Content))]
    public Ulid? ContentId { get; set; }

    #region Navigation properties

    [DeleteBehavior(DeleteBehavior.Cascade)]
    public CourseChapterModel Chapter { get; set; } = default!;

    [InverseProperty(nameof(CourseMaterialContentModel.Material))]
    public CourseMaterialContentModel Content { get; set; } = default!;

    public ICollection<CourseMaterialReactionModel> Reactions { get; set; } = default!;

    #endregion
}
