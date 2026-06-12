using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;

namespace Noo.Api.Courses.Models;

[Model("course_chapter")]
public class CourseChapterModel : OrderedModel
{
    [Required]
    [Column("title", TypeName = DbDataTypes.Varchar255)]
    [MinLength(1)]
    [MaxLength(255)]
    public string Title { get; set; } = default!;

    [Column("color", TypeName = DbDataTypes.Varchar63)]
    [MaxLength(63)]
    public string? Color { get; set; }

    [Column("is_active", TypeName = DbDataTypes.Boolean)]
    [Required]
    public bool IsActive { get; set; }

    [Column("publish_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime? PublishAt { get; set; }

    [Column("course_id", TypeName = DbDataTypes.Ulid)]
    [Required]
    [ForeignKey(nameof(Course))]
    public Ulid CourseId { get; set; }

    [Column("parent_chapter_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(ParentChapter))]
    public Ulid? ParentChapterId { get; set; }

    #region Navigation properties

    [InverseProperty(nameof(CourseModel.Chapters))]
    public CourseModel Course { get; set; } = default!;

    [InverseProperty(nameof(SubChapters))]
    public CourseChapterModel? ParentChapter { get; set; }

    [DeleteBehavior(DeleteBehavior.Cascade)]
    public ICollection<CourseChapterModel> SubChapters { get; set; } = [];

    [InverseProperty(nameof(CourseMaterialModel.Chapter))]
    public ICollection<CourseMaterialModel> Materials { get; set; } = [];

    #endregion
}
