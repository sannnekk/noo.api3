using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Courses.Models;
using Noo.Api.Media.Models;
using Noo.Api.NooTube.Types;
using Noo.Api.Users.Models;

namespace Noo.Api.NooTube.Models;

[Model("nootube_video")]
public class NooTubeVideoModel : BaseModel
{
    [Column("title", TypeName = DbDataTypes.Varchar255)]
    [Required]
    [MinLength(1)]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Column("description", TypeName = DbDataTypes.Varchar512)]
    [MaxLength(512)]
    public string? Description { get; set; }

    [Column("thumbnail_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Thumbnail))]
    public Ulid? ThumbnailId { get; set; }

    [Column("external_identifier", TypeName = DbDataTypes.Varchar255)]
    [MaxLength(255)]
    public string? ExternalIdentifier { get; set; }

    [Column("external_url", TypeName = DbDataTypes.Varchar512)]
    [MaxLength(512)]
    public string? ExternalUrl { get; set; }

    [Column("external_thumbnail_url", TypeName = DbDataTypes.Varchar512)]
    [MaxLength(512)]
    public string? ExternalThumbnailUrl { get; set; }

    [Column("service_type", TypeName = NooTubeDbEnumDataTypes.NooTubeServiceType)]
    [Required]
    public NooTubeServiceType ServiceType { get; set; }

    [Column("state", TypeName = NooTubeDbEnumDataTypes.VideoState)]
    [Required]
    public VideoState State { get; set; } = VideoState.NotUploaded;

    [Column("duration", TypeName = DbDataTypes.MediumIntUnsigned)]
    [Range(0, 16_777_215)]
    public int? Duration { get; set; }

    [Column("published_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime? PublishedAt { get; set; }

    [Column("uploaded_by_user_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(UploadedByUser))]
    public Ulid? UploadedByUserId { get; set; }

    #region Navigation Properties

    [InverseProperty(nameof(UserModel.UploadedVideos))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public UserModel UploadedByUser { get; set; } = default!;

    [InverseProperty(nameof(MediaModel.NooTubeVideoThumbnail))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public MediaModel? Thumbnail { get; set; }

    public ICollection<NooTubeVideoCommentModel> Comments { get; set; } = [];

    public ICollection<NooTubeVideoReactionModel> Reactions { get; set; } = [];

    public ICollection<CourseMaterialContentModel> CourseMaterialContents { get; set; } = [];

    #endregion
}
