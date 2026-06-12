using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Media.Models;
using Noo.Api.Users.Types;

namespace Noo.Api.Users.Models;

[Model("user_avatar")]
public class UserAvatarModel : BaseModel
{
    [Column("avatar_type", TypeName = UserEnumDbDataTypes.UserAvatarType)]
    [Required]
    public UserAvatarType AvatarType { get; set; } = UserAvatarType.None;

    [Column("avatar_url", TypeName = DbDataTypes.Varchar255)]
    [MaxLength(255)]
    public string AvatarUrl { get; set; } = string.Empty;

    [Column("media_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Media))]
    public Ulid? MediaId { get; set; }

    [Column("user_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(User))]
    [Required]
    public Ulid UserId { get; set; }

    #region Navigation Properties

    [InverseProperty(nameof(MediaModel.UserAvatar))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public MediaModel? Media { get; set; }

    [DeleteBehavior(DeleteBehavior.Cascade)]
    [InverseProperty(nameof(UserModel.Avatar))]
    public UserModel User { get; set; } = null!;

    #endregion
}
