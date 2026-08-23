using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Courses.Models;
using Noo.Api.Media.Models;
using Noo.Api.Users.Models;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.MediaDownloads.Models;

/// <summary>
/// One recorded download of a file. Append-only.
/// </summary>
/// <remarks>
/// The indexes pair each lookup column with <c>Id</c> because ULIDs are time-ordered and
/// <c>ApplyDefaultOrdering</c> sorts by <c>Id DESC</c>: one index then serves both the filter and
/// the sort, keeping a page's cost independent of how large the table has grown.
///
/// There is deliberately no separate timestamp column — <see cref="BaseModel.CreatedAt"/> already
/// records when the download happened, and these rows are never updated.
/// </remarks>
[Model("media_download")]
[Index(nameof(MediaId), nameof(Id))]
[Index(nameof(UserId), nameof(Id))]
[Index(nameof(CourseMaterialId), nameof(Id))]
public class MediaDownloadModel : BaseModel
{
    [Column("media_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Media))]
    [Required]
    public Ulid MediaId { get; set; }

    [Column("user_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(User))]
    [Required]
    public Ulid UserId { get; set; }

    /// <summary>
    /// The material the file was attached to when it was downloaded. Captured at write time so the
    /// download stays attributable after a teacher detaches the file from the material.
    /// </summary>
    [Column("course_material_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(CourseMaterial))]
    public Ulid? CourseMaterialId { get; set; }

    #region Navigation Properties

    [DeleteBehavior(DeleteBehavior.Cascade)]
    public MediaModel Media { get; set; } = default!;

    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel User { get; set; } = default!;

    [DeleteBehavior(DeleteBehavior.SetNull)]
    public CourseMaterialModel? CourseMaterial { get; set; }

    #endregion
}
