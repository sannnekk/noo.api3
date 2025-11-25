using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Courses.Models;
using Noo.Api.NooTube.Models;
using Noo.Api.Users.Models;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.Media.Models;

[Model("media")]
[Index(nameof(Hash), IsUnique = false)]
public class MediaModel : OrderedModel
{
    [Column("hash", TypeName = DbDataTypes.Varchar512)]
    [MaxLength(512)]
    public string Hash { get; set; } = string.Empty;

    [Column("path", TypeName = DbDataTypes.Varchar255)]
    [MaxLength(255)]
    [Required]
    public string Path { get; set; } = string.Empty;

    [Column("name", TypeName = DbDataTypes.Varchar255)]
    [MaxLength(255)]
    [Required]
    public string Name { get; set; } = string.Empty;

    [Column("actual_name", TypeName = DbDataTypes.Varchar255)]
    [MaxLength(255)]
    public string ActualName { get; set; } = string.Empty;

    [Column("extension", TypeName = DbDataTypes.Varchar15)]
    [MaxLength(15)]
    [Required]
    public string Extension { get; set; } = string.Empty;

    [Column("size", TypeName = DbDataTypes.Int)]
    public long Size { get; set; }

    [Column("reason", TypeName = "varchar(63)")]
    public string Reason { get; set; } = string.Empty;

    [Column("entity_id", TypeName = DbDataTypes.Ulid)]
    public Ulid? EntityId { get; set; }

    [Column("owner_id", TypeName = DbDataTypes.Ulid)]
    public Ulid? OwnerId { get; set; }

    #region Navigation Properties

    public ICollection<CourseModel> Courses { get; set; } = [];

    public NooTubeVideoModel? NooTubeVideoThumbnail { get; set; }

    public UserAvatarModel? UserAvatar { get; set; }

    public ICollection<CourseMaterialContentModel>? CourseMaterialContents { get; set; }

    #endregion
}
