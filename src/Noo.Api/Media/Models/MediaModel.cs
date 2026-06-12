using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Courses.Models;
using Noo.Api.Media.Types;
using Noo.Api.NooTube.Models;
using Noo.Api.Users.Models;
using Noo.Api.UserSettings.Models;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.Media.Models;

[Model("media")]
[Index(nameof(Hash), IsUnique = false)]
[Index(nameof(Category), IsUnique = false)]
[Index(nameof(OwnerId), IsUnique = false)]
public class MediaModel : OrderedModel
{
    [Column("hash", TypeName = DbDataTypes.Varchar512)]
    [MaxLength(512)]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Bucket-relative S3 object key.
    /// </summary>
    [Column("path", TypeName = DbDataTypes.Varchar255)]
    [MaxLength(255)]
    [Required]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Sanitized name used internally (e.g. for the S3 object).
    /// </summary>
    [Column("name", TypeName = DbDataTypes.Varchar255)]
    [MaxLength(255)]
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Original file name as provided by the uploader.
    /// </summary>
    [Column("actual_name", TypeName = DbDataTypes.Varchar255)]
    [MaxLength(255)]
    public string ActualName { get; set; } = string.Empty;

    [Column("extension", TypeName = DbDataTypes.Varchar15)]
    [MaxLength(15)]
    [Required]
    public string Extension { get; set; } = string.Empty;

    [Column("size", TypeName = DbDataTypes.Int)]
    public long Size { get; set; }

    [Required]
    [Column("category", TypeName = MediaEnumDbDataTypes.MediaCategory)]
    public MediaCategory Category { get; set; }

    [Required]
    [Column("status", TypeName = MediaEnumDbDataTypes.MediaStatus)]
    public MediaStatus Status { get; set; } = MediaStatus.Pending;

    /// <summary>
    /// Optional id of the entity this media is attached to
    /// </summary>
    [Column("entity_id", TypeName = DbDataTypes.Ulid)]
    public Ulid? EntityId { get; set; }

    [Required]
    [Column("owner_id", TypeName = DbDataTypes.Ulid)]
    public Ulid OwnerId { get; set; }

    #region Navigation Properties

    public ICollection<CourseModel> Courses { get; set; } = [];

    public NooTubeVideoModel? NooTubeVideoThumbnail { get; set; }

    public UserAvatarModel? UserAvatar { get; set; }

    public ICollection<CourseMaterialContentModel>? CourseMaterialContents { get; set; }

    public UserSettingsModel? UserSettingsWithBackgroundImage { get; set; }

    #endregion
}
