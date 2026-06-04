using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.Json;
using Noo.Api.Users.Models;

namespace Noo.Api.Notifications.Models;

[Model("notification")]
public class NotificationModel : BaseModel
{
    [Column("user_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(User))]
    [Required]
    public Ulid UserId { get; set; }

    [Column("type", TypeName = DbDataTypes.Varchar63)]
    [MaxLength(63)]
    [Required]
    public string Type { get; set; } = string.Empty;

    [Column("title", TypeName = DbDataTypes.Varchar127)]
    [MaxLength(127)]
    [Required]
    public string Title { get; set; } = string.Empty;

    [Column("message", TypeName = DbDataTypes.Varchar512)]
    [MaxLength(512)]
    [Required]
    public string Message { get; set; } = string.Empty;

    [Column("is_read", TypeName = DbDataTypes.Boolean)]
    [Required]
    public bool IsRead { get; set; }

    [Column("is_banner", TypeName = DbDataTypes.Boolean)]
    [Required]
    public bool IsBanner { get; set; }

    [JsonColumn("link", Converter = typeof(FrontendLinkConverter))]
    public FrontendLink? Link { get; set; }

    [Column("link_text", TypeName = DbDataTypes.Varchar63)]
    [MaxLength(63)]
    public string? LinkText { get; set; }

    #region Navigation Props

    [InverseProperty(nameof(UserModel.Notifications))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel User { get; set; } = default!;

    #endregion
}
