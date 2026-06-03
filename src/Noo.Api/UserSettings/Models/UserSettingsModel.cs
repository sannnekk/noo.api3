using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Media.Models;
using Noo.Api.Media.Services;
using Noo.Api.Users.Models;
using Noo.Api.UserSettings.Types;

namespace Noo.Api.UserSettings.Models;

[Model("user_settings")]
public class UserSettingsModel : BaseModel, IHasPresignedMedia
{
    [Column("user_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(User))]
    [Required]
    public Ulid UserId { get; set; }

    [Column("theme", TypeName = UserSettingsEnumDbTypes.UserTheme)]
    public UserTheme? Theme { get; set; }

    [Column("font_size", TypeName = UserSettingsEnumDbTypes.FontSize)]
    public string? FontSize { get; set; }

    [Column("background_image_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(BackgroundImage))]
    public Ulid? BackgroundImageId { get; set; }

    #region Navigation Properties

    [InverseProperty(nameof(UserModel.Settings))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel User { get; set; } = default!;

    [InverseProperty(nameof(MediaModel.UserSettingsWithBackgroundImage))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public MediaModel? BackgroundImage { get; set; }

    #endregion

    public IEnumerable<MediaModel?> GetMediaForPresigning()
    {
        yield return BackgroundImage;
    }
}
