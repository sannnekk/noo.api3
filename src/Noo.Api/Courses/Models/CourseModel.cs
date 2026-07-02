using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Media.Models;
using Noo.Api.Subjects.Models;
using Noo.Api.Users.Models;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.Courses.Models;

[Model("course")]
[Index(nameof(Name), IsUnique = false)]
public class CourseModel : BaseModel, ISoftDeletableModel
{
    [Required]
    [Column("name", TypeName = DbDataTypes.Varchar255)]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("start_date", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime? StartDate { get; set; }

    [Column("end_date", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime? EndDate { get; set; }

    [Column("description", TypeName = DbDataTypes.Text)]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column("thumbnail_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Thumbnail))]
    public Ulid? ThumbnailId { get; set; }

    [Column("subject_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Subject))]
    public Ulid? SubjectId { get; set; }

    [Column("is_deleted", TypeName = DbDataTypes.Boolean)]
    public bool IsDeleted { get; set; }

    [Column("is_archived", TypeName = DbDataTypes.Boolean)]
    public bool IsArchived { get; set; }

    #region Navigation Properties

    [DeleteBehavior(DeleteBehavior.SetNull)]
    [InverseProperty(nameof(MediaModel.Courses))]
    public MediaModel? Thumbnail { get; set; }

    [InverseProperty(nameof(CourseChapterModel.Course))]
    public ICollection<CourseChapterModel> Chapters { get; set; } = [];

    [InverseProperty(nameof(UserModel.CoursesAsEditor))]
    public ICollection<UserModel> Editors { get; set; } = [];

    [InverseProperty(nameof(UserModel.CoursesAsAuthor))]
    public ICollection<UserModel> Authors { get; set; } = [];

    [InverseProperty(nameof(CourseMembershipModel.Course))]
    public ICollection<CourseMembershipModel> Memberships { get; set; } = [];

    [InverseProperty(nameof(SubjectModel.Courses))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public SubjectModel? Subject { get; set; }

    #endregion

}
