using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Users.Models;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.NooTube.Models;

[Model("nootube_video_favourite")]
[Index(nameof(UserId), nameof(VideoId), IsUnique = true)]
public class NooTubeVideoFavouriteModel : BaseModel
{
    [Column("user_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(User))]
    [Required]
    public Ulid UserId { get; set; }

    [Column("video_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Video))]
    [Required]
    public Ulid VideoId { get; set; }

    #region Navigation Properties

    [InverseProperty(nameof(UserModel.NooTubeVideoFavourites))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel User { get; set; } = default!;

    [InverseProperty(nameof(NooTubeVideoModel.Favourites))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public NooTubeVideoModel Video { get; set; } = default!;

    #endregion
}
